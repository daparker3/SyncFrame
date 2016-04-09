//-----------------------------------------------------------------------
// <copyright file="MessageTransport.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using EnsureThat;
    using Properties;

    /// <summary>
    /// A message transport represents the base messaging functionality for a client or server.
    /// </summary>
    public class MessageTransport : IDisposable
    {
        private const int SerializationFactor = 2;
        private TaskCompletionSource<Task> faultingTaskTcs;
        private CancellationToken connectionClosedToken;
        private int requestId = int.MinValue;
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
            this.faultingTaskTcs = new TaskCompletionSource<Task>();
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
        public Task<Result> SendData<TRequest>(TRequest data) where TRequest : class
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Receives the data.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <returns>A <see cref="Task{Result}"/> which completes with a <see cref="TypedResult{TResult}"/> when the data is available.</returns>
        public Task<TypedResult<TResponse>> ReceiveData<TResponse>() where TResponse : class
        {
            throw new NotImplementedException();
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
        /// <exception cref="InvalidOperationException">Occurs inside an <see cref="AggregateException"/> if the transport was already opened.</exception>
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

                this.connectionClosedToken.Register(this.ConnectionClosedHandler);
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

        internal Task<Result> SendData<TRequest>(Result result, TRequest data, bool fault) where TRequest : class
        {
            Ensure.That(result, "result").IsNotNull();
            Ensure.That(result.Remote, "result.Remote").IsTrue();
            ////return Task.Factory.StartNew(() =>
            ////{
            ////    Ensure.That(data, "data").IsNotNull();
            ////    throw new NotImplementedException();
            ////});
            throw new NotImplementedException();
        }

        internal Task<TypedResult<TResponse>> ReceiveData<TResponse>(Result result) where TResponse : class
        {
            Ensure.That(result, "result").IsNotNull();
            Ensure.That(result.Remote, "result.Remote").IsTrue();
            throw new NotImplementedException();
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
                }
            }
        }

        /// <summary>
        /// Reads the incoming messages from the remote stream.
        /// </summary>
        /// <returns>A task which when complete indicates the read period of the sync is done.</returns>
        protected Task ReadMessages()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Writes the outgoing messages to the remote stream.
        /// </summary>
        /// <returns>A task which when complete indicates the write period of the sync is done.</returns>
        protected Task WriteMessages()
        {
            throw new NotImplementedException();
        }

        private void ConnectionClosedHandler()
        {
            this.IsConnectionOpen = false;
            this.faultingTaskTcs.TrySetCanceled();
        }

        private Result CreateResult()
        {
            return new Result(this, ++this.requestId);
        }

        private TypedResult<TData> CreateResult<TData>(TData data) where TData : class
        {
            return new TypedResult<TData>(this, ++this.requestId, data);
        }
    }
}
