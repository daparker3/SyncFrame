//-----------------------------------------------------------------------
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
    /// <remarks>
    /// When an exception is listed in one of the documentation nodes for this class, it is possible that the exception
    /// will be thrown by itself, or inside an <see cref="AggregateException"/>.
    /// </remarks>
    public class MessageTransport : IDisposable
    {
        private const int SerializationSizingConstant = 2;
        private const int SizeRequestChunkRetryBuckets = 10;
        private TaskCompletionSource<Task> faultingTaskTcs = new TaskCompletionSource<Task>();
        private ConcurrentBag<QueuedRequestChunk>[] queuedRequestChunks = new ConcurrentBag<QueuedRequestChunk>[SizeRequestChunkRetryBuckets];
        private ConcurrentDictionary<long, WeakReference<QueuedResponseChunk>> pendingResponsesByRequest = new ConcurrentDictionary<long, WeakReference<QueuedResponseChunk>>();
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
        /// <exception cref="OperationCanceledException">Occurs if the operation was canceled.</exception>
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
        /// <exception cref="OperationCanceledException">Occurs if the session was canceled.</exception>
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
            return Task.Factory.StartNew(async () =>
            {
                Ensure.That(data, "data").IsNotNull();
                TypedResult<TRequest> response = new TypedResult<TRequest>(this, request.RequestId, data);
                QueuedRequestChunk qrc = new QueuedRequestChunk(SerializationSizingConstant * Marshal.SizeOf(data));
                if (!request.Remote)
                {
                    // This request originates from us; set up our response handler.
                    QueuedResponseChunk responseChunk = new QueuedResponseChunk();

                    //// To prevent the response chunk going out of scope before the user can get it, we reference it in our
                    //// return value. That way, if it actually does go out of scope we can catch it with a runtime error.
                    response.ResponseChunk = responseChunk;
                    if (!this.pendingResponsesByRequest.TryAdd(request.RequestId, new WeakReference<QueuedResponseChunk>(responseChunk)))
                    {
                        throw new InvalidOperationException(Resources.TheResponseWasCompletedMultipleTimes);
                    }
                }

                try
                {
                    response.Write(qrc.DataStream, data, fault);
                    this.queuedRequestChunks[0].Add(qrc);
                    await qrc.RequestCompleteTask.Task;
                    return (Result)response;
                }
                catch (Exception)
                {
                    this.CompleteResponse(request.RequestId);
                    throw;
                }
            }).Unwrap();
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

                WeakReference<QueuedResponseChunk> wrQrc;
                if (!this.pendingResponsesByRequest.TryGetValue(result.RequestId, out wrQrc))
                {
                    throw new InvalidOperationException(Resources.TheResponseWasCompletedMultipleTimes);
                }

                QueuedResponseChunk qrc;
                if (!wrQrc.TryGetTarget(out qrc))
                {
                    throw new InvalidOperationException(Resources.ARequestOfTypeLeaked);
                }

                try
                {
                    return await this.ReceiveData<TResponse>(qrc, token);
                }
                catch (Exception)
                {
                    this.CompleteResponse(result.RequestId);
                    throw;
                }
            }).Unwrap();
        }

        internal bool CompleteResponse(long requestId)
        {
            WeakReference<QueuedResponseChunk> qrc;
            return this.pendingResponsesByRequest.TryRemove(requestId, out qrc);
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
                try
                {
                    Task leakedRequestCheckTack = this.LeakedRequestCheck();
                    FrameHeader frameHeader = Serializer.DeserializeWithLengthPrefix<FrameHeader>(this.RemoteStream, PrefixStyle.Base128);
                    for (int i = 0; i < frameHeader.MessageSizes.Length; ++i)
                    {
                        byte[] bData = new byte[frameHeader.MessageSizes[i]];
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
                            WeakReference<QueuedResponseChunk> wrQrc;
                            if (this.pendingResponsesByRequest.TryGetValue(header.RequestId, out wrQrc))
                            {
                                wrQrc.TryGetTarget(out qrc);
                            }
                        }
                        else
                        {
                            qrc = this.pendingResponsesByType[header.DataType];
                        }

                        if (qrc != null)
                        {
                            // Cool. We can dispatch this request.
                            await qrc.DataStream.ReadAsync(bData, (int)startData, (int)(bData.LongLength - startData));
                            qrc.RequestCompleteTask.SetResult(0);
                            qrc.Dispose();
                        }
                    }

                    await leakedRequestCheckTack;
                }
                catch (Exception)
                {
                    this.CancelTaskCompletionSources();
                    throw;
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
                try
                {
                    long written = sizeof(long);
                    long max = this.MaxFrameSize;
                    if (written >= max)
                    {
                        throw new InternalBufferOverflowException();
                    }

                    List<int> chunkSizes = new List<int>();
                    List<QueuedRequestChunk> outputChunks = new List<QueuedRequestChunk>();

                    for (int j = this.queuedRequestChunks.Length - 1; j >= 0; --j)
                    {
                        int count = this.queuedRequestChunks[0].Count;
                        for (int i = 0; written < max && i < count; ++i)
                        {
                            QueuedRequestChunk chunk;
                            if (!this.queuedRequestChunks[i].TryTake(out chunk))
                            {
                                break;
                            }

                            int toWrite = (int)chunk.DataStream.Length;
                            if (written + toWrite > max)
                            {
                                if (written == sizeof(long))
                                {
                                    // Uh oh, we can't write this chunk. 
                                    throw new InternalBufferOverflowException();
                                }

                                // Okay, we'll just re-queue this chunk for later.
                                if (i == this.queuedRequestChunks.Length - 1)
                                {
                                    this.queuedRequestChunks[i].Add(chunk);
                                }
                                else
                                {
                                    this.queuedRequestChunks[i + 1].Add(chunk);
                                }
                            }
                            else
                            {
                                written += toWrite;
                                outputChunks.Add(chunk);
                            }
                        }
                    }

                    FrameHeader frameHeader = new FrameHeader();
                    frameHeader.MessageSizes = chunkSizes.ToArray();
                    Serializer.SerializeWithLengthPrefix(this.RemoteStream, frameHeader, PrefixStyle.Base128);
                    foreach (QueuedRequestChunk qrc in outputChunks)
                    {
                        await qrc.DataStream.CopyToAsync(this.RemoteStream);
                        qrc.RequestCompleteTask.SetResult(0);
                        qrc.DataStream.Dispose();
                    }

                    // After writing out our fault to the remote, we fault.
                    if (written > 0 && this.faultingTaskTcs.Task.IsCompleted)
                    {
                        this.faultingTaskTcs.Task.Wait();
                    }
                }
                catch (Exception)
                {
                    this.CancelTaskCompletionSources();
                    throw;
                }
            }).Unwrap();
        }

        private Task LeakedRequestCheck()
        {
            return Task.Factory.StartNew(() =>
            {
                // Check for user errors by seeing if any request that has not been responded to has leaked.
                foreach (long requestId in this.pendingResponsesByRequest.Keys)
                {
                    WeakReference<QueuedResponseChunk> wrQrc;
                    if (this.pendingResponsesByRequest.TryGetValue(requestId, out wrQrc))
                    {
                        QueuedResponseChunk qrc;
                        if (!wrQrc.TryGetTarget(out qrc))
                        {
                            throw new InvalidOperationException(Resources.ARequestOfTypeLeaked);
                        }
                    }
                }
            });
        }

        private void CancelTaskCompletionSources()
        {
            int canceled;
            do
            {
                canceled = 0;

                // Try to cancel any pending TCS objects.
                this.faultingTaskTcs.TrySetCanceled();
                for (int i = 0; i < this.queuedRequestChunks.Length; ++i)
                {
                    QueuedRequestChunk request;
                    while (this.queuedRequestChunks[i].TryTake(out request))
                    {
                        request.RequestCompleteTask.TrySetCanceled();
                        request.Dispose();
                        ++canceled;
                    }
                }

                foreach (long requestId in this.pendingResponsesByRequest.Keys)
                {
                    WeakReference<QueuedResponseChunk> responseWeakRef;
                    while (this.pendingResponsesByRequest.TryRemove(requestId, out responseWeakRef))
                    {
                        QueuedResponseChunk response;
                        if (responseWeakRef.TryGetTarget(out response))
                        {
                            response.RequestCompleteTask.TrySetCanceled();
                            response.Dispose();
                            ++canceled;
                        }
                    }
                }

                foreach (Type type in this.pendingResponsesByType.Keys)
                {
                    QueuedResponseChunk response;
                    while (this.pendingResponsesByType.TryRemove(type, out response))
                    {
                        response.RequestCompleteTask.TrySetCanceled();
                        response.Dispose();
                        ++canceled;
                    }
                }
            }
            while (canceled > 0);
        }

        private void ConnectionClosedHandler()
        {
            this.IsConnectionOpen = false;
            this.CancelTaskCompletionSources();
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

                    return new TypedResult<TResponse>(this, qrc.Header, qrc.DataStream);
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
    }
}
