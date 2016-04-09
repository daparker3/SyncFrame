﻿//-----------------------------------------------------------------------
// <copyright file="MessageTransport.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using EnsureThat;
    using Properties;
    using ProtoBuf;

    /// <summary>
    /// A message transport represents the base messaging functionality for a client or server.
    /// </summary>
    public class MessageTransport : IDisposable
    {
        private const int SerializationSizingConstant = 2;
        private TaskCompletionSource<Task> faultingTaskTcs = new TaskCompletionSource<Task>();
        private ConcurrentQueue<QueuedRequestChunk> queuedRequestChunks = new ConcurrentQueue<QueuedRequestChunk>();
        private ConcurrentBag<long> inProgressRequests = new ConcurrentBag<long>();
        private ConcurrentDictionary<long, QueuedResponseChunk> pendingResponsesByRequest = new ConcurrentDictionary<long, QueuedResponseChunk>();
        private ConcurrentDictionary<Type, QueuedResponseChunk> pendingResponsesByType = new ConcurrentDictionary<Type, QueuedResponseChunk>();
        private CancellationToken connectionClosedToken;
        private CancellationTokenRegistration connectionClosedTokenRegistration;
        private long currentRequest = 0;
        private bool opened = false;
        private bool disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageTransport"/> class.
        /// </summary>
        /// <param name="remoteStream">The remote stream.</param>
        /// <param name="token">An optional <see cref="CancellationToken"/> which can be used to close the session.</param>
        protected MessageTransport(Stream remoteStream, CancellationToken token)
        {
            Ensure.That(remoteStream, "remoteStream").IsNotNull();
            this.RemoteStream = remoteStream;
            this.MaxFrameSize = -1;
            this.connectionClosedToken = token;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="MessageTransport"/> class.
        /// </summary>
        ~MessageTransport()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Gets a value indicating whether this instance is open.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is open; otherwise, <c>false</c>.
        /// </value>
        public bool IsConnectionOpen
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the maximum size of the frame.
        /// </summary>
        /// <value>
        /// The maximum transmitted size of a sync frame before fragmentation occurs in bytes.
        /// </value>
        /// <remarks>
        /// The default value, -1, indicates that no message will be fragmented. 
        /// It's recommended that you leave it at this value unless you're dealing with maintaining a stable connection over a low throughput connection.
        /// </remarks>
        public int MaxFrameSize
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the connection closed token.
        /// </summary>
        /// <value>
        /// The connection closed token.
        /// </value>
        protected CancellationToken ConnectionClosedToken
        {
            get
            {
                return this.connectionClosedToken;
            }
        }

        /// <summary>
        /// Gets the remote stream.
        /// </summary>
        /// <value>
        /// The remote stream.
        /// </value>
        protected Stream RemoteStream
        {
            get;
            private set;
        }

        /// <summary>
        /// Sends the data.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request.</typeparam>
        /// <param name="data">The data.</param>
        /// <returns>A <see cref="Task{Result}"/> which completes with a <see cref="Result"/> when sent.</returns>
        /// <exception cref="InvalidOperationException">Thrown if an invalid parameter is received.</exception>
        /// <exception cref="InternalBufferOverflowException">Thrown if the maximum frame size is too small to fit any request.</exception>
        public Task<Result> SendData<TRequest>(TRequest data) where TRequest : class
        {
            return this.SendData(this.CreateResult(), data, false);
        }

        /// <summary>
        /// Receives the data.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <returns>A <see cref="Task{Result}"/> which completes with a <see cref="TypedResult{TResult}"/> when the data is available.</returns>
        /// <exception cref="InvalidOperationException">Thrown if an invalid parameter is received.</exception>
        public Task<TypedResult<TResponse>> ReceiveData<TResponse>() where TResponse : class
        {
            return this.ReceiveData<TResponse>(CancellationToken.None);
        }

        /// <summary>
        /// Receives the data.
        /// </summary>
        /// <param name="token">An optional cancellation token.</param>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <returns>A <see cref="Task{Result}"/> which completes with a <see cref="TypedResult{TResult}"/> when the data is available.</returns>
        /// <exception cref="InvalidOperationException">Thrown if an invalid parameter is received.</exception>
        public Task<TypedResult<TResponse>> ReceiveData<TResponse>(CancellationToken token) where TResponse : class
        {
            return Task.Factory.StartNew(async () =>
            {
                Type responseType = typeof(TResponse);
                if (this.pendingResponsesByType.ContainsKey(responseType))
                {
                    throw new InvalidOperationException(Resources.RequestAlreadyInProgress);
                }

                QueuedResponseChunk qrc = new QueuedResponseChunk();
                this.pendingResponsesByType[responseType] = qrc;
                try
                {
                    return await this.ReceiveData<TResponse>(qrc, token);
                }
                finally
                {
                    QueuedResponseChunk outQrc;
                    if (!this.pendingResponsesByType.TryRemove(responseType, out outQrc))
                    {
                        throw new InvalidOperationException();
                    }
                }
            }).Unwrap();
        }

        /// <summary>
        /// Called when a remote or local session fault occurs.
        /// </summary>
        /// <returns>A task which can be waited on to yield the faulting task for the session.</returns>
        /// <remarks>
        /// When a fault is reported for a session, the session is guaranteed to terminate. 
        /// This means any pending responses to the client, with the exception of the fault, will be canceled.
        /// The faulting task can be waited on to retrieve the <see cref="FaultException{TFault}"/> which terminated the session.
        /// If the session is closed by canceling the <see cref="CancellationToken"/> argument, the resulting task from calling
        /// this method may be put into the canceled state without yielding a <see cref="Task"/> value.
        /// </remarks>
        public Task<Task> OnFault()
        {
            return this.faultingTaskTcs.Task;
        }

        /// <summary>
        /// Opens the connection and begins listening for messages.
        /// </summary>
        /// <returns>A task which when complete indicates the transport has completed the session.</returns>
        /// <remarks>This can be overridden in child classes to provide additional open behavior.</remarks>
        /// <exception cref="InvalidOperationException">Occurs if the transport was already opened.</exception>
        public virtual Task Open()
        {
            bool alreadyOpened = this.opened;
            this.opened = true;
            return Task.Factory.StartNew(() =>
            {
                if (alreadyOpened)
                {
                    throw new InvalidOperationException(Resources.ConnectionAlreadyOpened);
                }

                this.connectionClosedTokenRegistration = this.connectionClosedToken.Register(this.ConnectionClosedHandler);
                this.IsConnectionOpen = true;
            });
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal void SetFault<TFaultException>(TFaultException faultEx) where TFaultException : Exception
        {
            Ensure.That(faultEx, "faultEx").IsNotNull();
            this.faultingTaskTcs.SetException(faultEx);
        }

        internal Task<Result> SendData<TRequest>(Result result, TRequest data) where TRequest : class
        {
            return this.SendData(result, data, false);
        }

        internal Task<Result> SendData<TRequest>(Result request, TRequest data, bool fault) where TRequest : class
        {
            Ensure.That(request, "result").IsNotNull();
            Ensure.That(request.Remote, "result.Remote").IsTrue();
            return this.SendData(request.RequestId, data, fault);
        }

        internal Task<TypedResult<TResponse>> ReceiveData<TResponse>(Result result, CancellationToken token) where TResponse : class
        {
            Ensure.That(result, "result").IsNotNull();
            Ensure.That(result.Remote, "result.Remote").IsTrue();
            return Task.Factory.StartNew(async () =>
            {
                if (this.pendingResponsesByRequest.ContainsKey(result.RequestId))
                {
                    throw new InvalidOperationException(Resources.RequestAlreadyInProgress);
                }

                try
                {
                    QueuedResponseChunk qrc = new QueuedResponseChunk();
                    this.pendingResponsesByRequest[result.RequestId] = qrc;
                    return await this.ReceiveData<TResponse>(qrc, token);
                }
                finally
                {
                    QueuedResponseChunk outQrc;
                    if (!this.pendingResponsesByRequest.TryRemove(result.RequestId, out outQrc))
                    {
                        throw new InvalidOperationException();
                    }
                }
            }).Unwrap();
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                this.disposed = true;

                if (disposing)
                {
                    if (this.RemoteStream != null)
                    {
                        this.RemoteStream.Close();
                        this.RemoteStream.Dispose();
                        this.RemoteStream = null;
                    }

                    this.connectionClosedTokenRegistration.Dispose();
                }
            }
        }

        /// <summary>
        /// Reads the incoming messages from the remote stream.
        /// </summary>
        /// <returns>A task which when complete indicates the read period of the sync is done.</returns>
        protected Task ReadMessages()
        {
            return Task.Factory.StartNew(async () =>
            {
                byte[] bToRead = new byte[sizeof(long)];
                await Task.Factory.FromAsync(this.RemoteStream.BeginRead, this.RemoteStream.EndRead, bToRead, 0, bToRead.Length, null);
                long toRead = BitConverter.ToInt64(bToRead, 0);
                toRead -= sizeof(long);
                while (toRead > 0)
                {
                    byte[] bChunkSz = new byte[sizeof(int)];
                    await Task.Factory.FromAsync(this.RemoteStream.BeginRead, this.RemoteStream.EndRead, bChunkSz, 0, bChunkSz.Length, null);
                    int chunkSz = BitConverter.ToInt32(bChunkSz, 0);
                    byte[] bData = new byte[sizeof(int)];
                    await Task.Factory.FromAsync(this.RemoteStream.BeginRead, this.RemoteStream.EndRead, bData, 0, bData.Length, null);
                    MessageHeader header;
                    long startData;
                    using (MemoryStream ms = new MemoryStream(bData))
                    {
                        header = Serializer.Deserialize<MessageHeader>(ms);
                        startData = ms.Position;
                    }

                    // Now that we have the header, we can figure out what to dispatch it to (if anything)
                    QueuedResponseChunk qrc = null;
                    if (this.pendingResponsesByRequest.ContainsKey(header.RequestId))
                    {
                        qrc = this.pendingResponsesByRequest[header.RequestId];
                    }
                    else
                    {
                        qrc = this.pendingResponsesByType[header.DataType];
                    }

                    if (qrc != null)
                    {
                        // Cool. We can dispatch this request.
                        qrc.Data = new byte[bData.Length - startData];
                        bData.CopyTo(qrc.Data, startData);
                        qrc.RequestCompleteTask.SetResult(0);
                    }
                }
            }).Unwrap();
        }

        /// <summary>
        /// Writes the outgoing messages to the remote stream.
        /// </summary>
        /// <returns>A task which when complete indicates the write period of the sync is done.</returns>
        protected Task WriteMessages()
        {
            return Task.Factory.StartNew(async () =>
            {
                int count = this.queuedRequestChunks.Count;
                List<QueuedRequestChunk> outputChunks = new List<QueuedRequestChunk>(count);
                long written = sizeof(long);
                long max = this.MaxFrameSize;
                if (written >= max)
                {
                    throw new InternalBufferOverflowException();
                }

                for (int i = 0; written < max && i < count; ++i)
                {
                    QueuedRequestChunk chunk;
                    if (!this.queuedRequestChunks.TryDequeue(out chunk))
                    {
                        break;
                    }

                    int toWrite = sizeof(int) + chunk.Data.Length;
                    if (written + toWrite > max)
                    {
                        if (written == sizeof(long))
                        {
                            // Uh oh, we can't write this chunk. 
                            throw new InternalBufferOverflowException();
                        }

                        // Okay, we'll just re-queue this chunk for later.
                        this.queuedRequestChunks.Enqueue(chunk);
                    }
                    else
                    {
                        written += toWrite;
                        outputChunks.Add(chunk);
                    }
                }

                byte[] bWritten = BitConverter.GetBytes(written);
                await Task.Factory.FromAsync(this.RemoteStream.BeginWrite, this.RemoteStream.EndWrite, bWritten, 0, bWritten.Length, null);
                foreach (QueuedRequestChunk qrc in outputChunks)
                {
                    byte[] bChunkSize = BitConverter.GetBytes(qrc.Data.Length);
                    await Task.Factory.FromAsync(this.RemoteStream.BeginWrite, this.RemoteStream.EndWrite, bChunkSize, 0, bChunkSize.Length, null);
                    await Task.Factory.FromAsync(this.RemoteStream.BeginWrite, this.RemoteStream.EndWrite, qrc.Data, 0, qrc.Data.Length, null);
                    qrc.RequestCompleteTask.SetResult(0);
                }

                // After writing out our fault to the remote, we fault.
                if (written > 0 && this.faultingTaskTcs.Task.IsCompleted)
                {
                    this.faultingTaskTcs.Task.Wait();
                }
            }).Unwrap();
        }

        private void ConnectionClosedHandler()
        {
            this.IsConnectionOpen = false;
            this.faultingTaskTcs.TrySetCanceled();
        }

        private Task<Result> SendData<TRequest>(long requestId, TRequest data, bool fault) where TRequest : class
        {
            return Task.Factory.StartNew(async () =>
            {
                Ensure.That(data, "data").IsNotNull();
                TypedResult<TRequest> response = new TypedResult<TRequest>(this, requestId, data);
                QueuedRequestChunk qrc = new QueuedRequestChunk();
                using (MemoryStream ms = new MemoryStream(SerializationSizingConstant * Marshal.SizeOf(data)))
                {
                    response.Write(ms, data, fault);
                    qrc.Data = ms.ToArray();
                }

                this.queuedRequestChunks.Enqueue(qrc);
                await qrc.RequestCompleteTask.Task;
                return (Result)response;
            }).Unwrap();
        }

        private Task<TypedResult<TResponse>> ReceiveData<TResponse>(QueuedResponseChunk qrc, CancellationToken token) where TResponse : class
        {
            return Task.Factory.StartNew(async () =>
            {
                TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
                using (CancellationTokenRegistration ctr = token.Register(() => tcs.SetCanceled()))
                {
                    Task waited = await Task.WhenAny(tcs.Task, qrc.RequestCompleteTask.Task);
                    if (waited == tcs.Task)
                    {
                        tcs.Task.Wait();
                        token.ThrowIfCancellationRequested(); 
                    }

                    using (MemoryStream ms = new MemoryStream(qrc.Data))
                    {
                        return new TypedResult<TResponse>(this, qrc.Header, ms);
                    }
                }
            }).Unwrap();
        }

        private Result CreateResult()
        {
            return new Result(this, ++this.currentRequest);
        }

        private TypedResult<TData> CreateResult<TData>(TData data) where TData : class
        {
            return new TypedResult<TData>(this, ++this.currentRequest, data);
        }

        private class QueuedRequestChunk
        {
            internal QueuedRequestChunk()
            {
                this.RequestCompleteTask = new TaskCompletionSource<int>();
            }

            internal byte[] Data { get; set; }

            internal TaskCompletionSource<int> RequestCompleteTask { get; private set; }
        }

        private class QueuedResponseChunk
        {
            internal QueuedResponseChunk()
            {
                this.RequestCompleteTask = new TaskCompletionSource<int>();
            }

            internal MessageHeader Header { get; set; }

            internal byte[] Data { get; set; }

            internal TaskCompletionSource<int> RequestCompleteTask { get; private set; }
        }
    }
}
