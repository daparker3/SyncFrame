//-----------------------------------------------------------------------
// <copyright file="MessageTransport.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System;
    using System.Collections.Generic;
    using System.IO;
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
        private TaskCompletionSource<Task> faultingTaskTcs = new TaskCompletionSource<Task>();
        private ConcurrentRequestResponseBuffer requestResponseBuffer = new ConcurrentRequestResponseBuffer();
        private ConcurrentRequestBuffer requestBuffer = new ConcurrentRequestBuffer();
        private ConcurrentResponseBuffer responseBuffer = new ConcurrentResponseBuffer();
        private CancellationToken connectionClosedToken;
        private CancellationTokenRegistration connectionClosedTokenRegistration;
        private long maxFrameSize = 0;
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
            this.MaxFrameSize = long.MaxValue;
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
        /// The default value of <see cref="long.MaxValue"/>  indicates that no outgoing data will be fragmented. 
        /// It's recommended that you leave it at this value unless you're having issues maintaining stable communication over a low throughput connection.
        /// </remarks>
        public long MaxFrameSize
        {
            get
            {
                return this.maxFrameSize;
            }

            set
            {
                Ensure.That(value, "value").IsGte(0);
                this.maxFrameSize = value;
            }
        }

        /// <summary>
        /// Gets the request count.
        /// </summary>
        /// <value>
        /// The request count.
        /// </value>
        public int RequestCount
        {
            get
            {
                return this.requestBuffer.Count;
            }
        }

        /// <summary>
        /// Gets the request response count.
        /// </summary>
        /// <value>
        /// The request response count.
        /// </value>
        public int RequestResponseCount
        {
            get
            {
                return this.requestResponseBuffer.Count;
            }
        }

        /// <summary>
        /// Gets the response buffer count.
        /// </summary>
        /// <value>
        /// The response buffer count.
        /// </value>
        public int ResponseBufferCount
        {
            get
            {
                return this.responseBuffer.Count;
            }
        }

        /// <summary>
        /// Gets the response buffer use.
        /// </summary>
        /// <value>
        /// The response buffer use.
        /// </value>
        public long BufferUse
        {
            get
            {
                return this.responseBuffer.BufferUse;
            }
        }

        /// <summary>
        /// Gets or sets the size of the response buffer.
        /// </summary>
        /// <value>
        /// The size of the response buffer.
        /// </value>
        /// <remarks>
        /// As the transport receives remote data, it will buffer the data in a structure until the user is able to receive it. 
        /// It's recommended the default response buffer value of <see cref="long.MaxValue"/> be set to something lower to prevent excessive buffering.
        /// When a new request causes the buffer to overflow, data will be randomly selected to be discarded until enough space exists in the buffer
        /// to receive the new data.
        /// </remarks>
        public long ResponseBufferSize
        {
            get
            {
                return this.responseBuffer.BufferSize;
            }

            set
            {
                Ensure.That(value, "value").IsGte(0);
                this.responseBuffer.BufferSize = value;
            }
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
        public async Task<RequestResult> SendData<TRequest>(TRequest data) where TRequest : class
        {
            return await this.SendData(this.CreateResult(), data, false);
        }

        /// <summary>
        /// Receives the data.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <returns>A <see cref="Task{Result}"/> which completes with a <see cref="TypedResult{TResult}"/> when the data is available.</returns>
        /// <exception cref="InvalidOperationException">Thrown if an invalid parameter is received.</exception>
        public async Task<TypedResult<TResponse>> ReceiveData<TResponse>() where TResponse : class
        {
            return await this.ReceiveData<TResponse>(CancellationToken.None);
        }

        /// <summary>
        /// Receives the data.
        /// </summary>
        /// <param name="token">An optional cancellation token.</param>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <returns>A <see cref="Task{Result}"/> which completes with a <see cref="TypedResult{TResult}"/> when the data is available.</returns>
        /// <exception cref="InvalidOperationException">Thrown if an invalid parameter is received.</exception>
        /// <exception cref="OperationCanceledException">Occurs if the operation was canceled.</exception>
        public async Task<TypedResult<TResponse>> ReceiveData<TResponse>(CancellationToken token) where TResponse : class
        {
            QueuedResponseChunk qrc = await this.responseBuffer.DequeueResponse(typeof(TResponse), token);
            return await this.ReceiveData<TResponse>(qrc);
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
            return Task.Run(() =>
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

        internal async Task<Result> SendData<TRequest>(Result result, TRequest data) where TRequest : class
        {
            RequestResult rr = await this.SendData(result, data, false);
            return (Result)rr;
        }

        internal async Task<RequestResult> SendData<TRequest>(Result request, TRequest data, bool fault) where TRequest : class
        {
            Ensure.That(request, "result").IsNotNull();
            Ensure.That(data, "data").IsNotNull();
            RequestResult requestResult = new RequestResult(this, request.RequestId);
            QueuedRequestChunk qrc = new QueuedRequestChunk();
            QueuedRequestResponseChunk responseChunk = null;

            if (!request.Remote)
            {
                responseChunk = this.requestResponseBuffer.CreateResponse(request.RequestId);
                requestResult.ResponseChunk = responseChunk;
            }

            try
            {
                requestResult.Write(qrc.DataStream, data, fault, request.Remote);
                this.requestBuffer.QueueRequest(qrc);
                return requestResult;
            }
            finally
            {
                await qrc.ResponseComplete();
                qrc.Dispose();
            }
        }

        internal async Task<TypedResult<TResponse>> ReceiveData<TResponse>(RequestResult result, CancellationToken token) where TResponse : class
        {
            Ensure.That(result, "result").IsNotNull();
            Ensure.That(result.Remote, "result.Remote").IsTrue();
            return await this.ReceiveData<TResponse>(result.ResponseChunk);
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

                    if (this.responseBuffer != null)
                    {
                        this.responseBuffer.Dispose();
                        this.responseBuffer = null;
                    }

                    this.connectionClosedTokenRegistration.Dispose();
                }
            }
        }

        /// <summary>
        /// Reads the incoming messages from the remote stream.
        /// </summary>
        /// <returns>A task which when complete indicates the read period of the sync is done.</returns>
        protected async Task ReadMessages()
        {
            try
            {
                FrameHeader frameHeader = Serializer.DeserializeWithLengthPrefix<FrameHeader>(this.RemoteStream, PrefixStyle.Base128);
                IEnumerator<long> sizesEnum = frameHeader.MessageSizes.GetEnumerator();
                while (sizesEnum.MoveNext())
                {
                    this.ConnectionClosedToken.ThrowIfCancellationRequested();
                    MessageHeader header = Serializer.Deserialize<MessageHeader>(this.RemoteStream);

                    // Now that we have the header, we can figure out what to dispatch it to (if anything)
                    if (header.Response)
                    {
                        QueuedRequestResponseChunk qrc;
                        if (!this.requestResponseBuffer.TryGetResponse(header.RequestId, out qrc))
                        {
                            throw new InvalidOperationException(Resources.NoSuchRequest);
                        }

                        await this.ReadHandler(sizesEnum.Current, header, qrc);
                    }
                    else
                    {
                        QueuedResponseChunk qrc = new QueuedResponseChunk();
                        await this.ReadHandler(sizesEnum.Current, header, qrc);
                        if (!await this.responseBuffer.QueueResponse(header.DataType, qrc, this.ConnectionClosedToken))
                        {
                            throw new InvalidOperationException(Resources.NoSuchRequest);
                        }
                    }
                }
            }
            catch (Exception)
            {
                this.CancelTaskCompletionSources();
                throw;
            }
        }

        /// <summary>
        /// Writes the outgoing messages to the remote stream.
        /// </summary>
        /// <returns>A task which when complete indicates the write period of the sync is done.</returns>
        protected async Task WriteMessages()
        {
            try
            {
                long written = sizeof(long);
                long max = this.MaxFrameSize;
                if (written >= max)
                {
                    throw new InternalBufferOverflowException();
                }

                FrameHeader frameHeader = new FrameHeader();
                frameHeader.MessageSizes = new List<long>();
                List<QueuedRequestChunk> outputChunks = new List<QueuedRequestChunk>();
                foreach (QueuedRequestChunk chunk in this.requestBuffer.DequeueRequests())
                {
                    long toWrite = chunk.DataStream.Length;
                    if (written + toWrite > max)
                    {
                        if (written == sizeof(long))
                        {
                            // Uh oh, we can't write this chunk. 
                            throw new InternalBufferOverflowException();
                        }

                        this.requestBuffer.RequeueRequest(chunk);
                    }
                    else
                    {
                        written += toWrite;
                        outputChunks.Add(chunk);
                        frameHeader.MessageSizes.Add(chunk.DataStream.Length);
                    }
                }

                Serializer.SerializeWithLengthPrefix(this.RemoteStream, frameHeader, PrefixStyle.Base128);
                foreach (QueuedRequestChunk qrc in outputChunks)
                {
                    await qrc.DataStream.CopyToAsync(this.RemoteStream);
                    qrc.Complete();
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
        }

        private void CancelTaskCompletionSources()
        {
            this.faultingTaskTcs.TrySetCanceled();
            this.requestBuffer.CancelRequests();
            this.responseBuffer.CancelResponses();
            this.requestResponseBuffer.CancelResponses();
        }

        private void ConnectionClosedHandler()
        {
            this.IsConnectionOpen = false;
            this.CancelTaskCompletionSources();
        }

        private async Task ReadHandler(long messageSize, MessageHeader header, QueuedResponseChunk qrc)
        {
            // Cool. We can dispatch this request.
            await this.RemoteStream.CopyToAsync(qrc.DataStream, (int)(messageSize - header.DataSize));
            qrc.Complete();
        }

        private async Task<TypedResult<TResponse>> ReceiveData<TResponse>(QueuedResponseChunk qrc) where TResponse : class
        {
            try
            {
                await qrc.ResponseComplete();
                return new TypedResult<TResponse>(this, qrc.Header, qrc.DataStream);
            }
            finally
            {
                qrc.Dispose();
            }
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
