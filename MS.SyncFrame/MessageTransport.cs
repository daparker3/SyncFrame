//-----------------------------------------------------------------------
// <copyright file="MessageTransport.cs" company="MS">
//     Copyright (c) 2016 MS.
// </copyright>
//-----------------------------------------------------------------------
namespace MS.SyncFrame
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using EnsureThat;

    /// <summary>
    /// A message transport represents the base messaging functionality for a client or server.
    /// </summary>
    public class MessageTransport : IDisposable
    {
        /// <summary>
        /// Whether or not the transport has been disposed.
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageTransport"/> class.
        /// </summary>
        /// <param name="remoteStream">The remote stream.</param>
        protected MessageTransport(Stream remoteStream)
        {
            Ensure.That(remoteStream, "remoteStream").IsNotNull();
            this.RemoteStream = remoteStream;
            this.MaxFrameSize = -1;
            this.IsConnectionOpen = true;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="MessageTransport"/> class.
        /// </summary>
        ~MessageTransport()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is open.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is open; otherwise, <c>false</c>.
        /// </value>
        public bool IsConnectionOpen
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the maximum size of the frame.
        /// </summary>
        /// <value>
        /// The maximum transmitted size of a sync frame before fragmentation occurs in bytes.
        /// </value>
        /// <remarks>The default value, -1, indicates that no message will be fragmented.</remarks>
        public int MaxFrameSize
        {
            get;
            set;
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
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        /// <remarks>
        /// If this task is started as a continuation of <see cref="MessageTransport.RecieveData{TResponse}"/>, 
        /// it will be treated as the completion of a call to <see cref="MessageTransport.SendData{TMessage}(TMessage)"/> made by the requester.
        /// Otherwise, it will be treated as a brand-new request.
        /// </remarks>
        /// <returns>A task which when waited on will complete when the message is handled remotely.</returns>
        public Task SendData<TMessage>(TMessage message)
        {
            // Send a message of the type TMessage to the server, 
            // completing the task when the message is sent.
            throw new NotImplementedException();
        }

        /// <summary>
        /// Receives the data.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <remarks>
        /// If this task is started as a continuation of <see cref="MessageTransport.SendData{TMessage}(TMessage)"/>, 
        /// it will be treated as the response handler of a call to <see cref="MessageTransport.RecieveData{TResponse}"/> made by the responder.
        /// Otherwise, it will be treated as a brand-new request.
        /// </remarks>
        /// <returns>A task which will waited on will yield the response.</returns>
        public Task<TResponse> RecieveData<TResponse>()
        {
            // Receive a message of the type TResponse from the server.
            throw new NotImplementedException();
        }

        /// <summary>
        /// Connects to a remotely hosted service.
        /// </summary>
        /// <typeparam name="TRemote">The type of the remote object being connected to.</typeparam>
        /// <param name="servicePath">The service path.</param>
        /// <returns>A task which when waited on yields a remote service proxy.</returns>
        public Task<TRemote> ConnectService<TRemote>(Uri servicePath)
        {
            // Connect to a remote service.
            throw new NotImplementedException();
        }

        /// <summary>
        /// Hosts a remote service listener.
        /// </summary>
        /// <typeparam name="TService">The type of the remote object being hosted.</typeparam>
        /// <param name="serviceObject">The service object.</param>
        /// <param name="servicePath">The service path.</param>
        /// <returns>A task which when complete indicates the hosting status of the service endpoint.</returns>
        public Task HostService<TService>(TService serviceObject, Uri servicePath)
        {
            // Host a service on the given message transport.
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called to subscribe to the next server fault of type <typeparamref name="TFault"/>.
        /// </summary>
        /// <typeparam name="TFault">The type of the fault being reported.</typeparam>
        /// <returns>A task which when waited on yields the next fault of type <typeparamref name="TFault"/></returns>
        /// <remarks>
        /// When a fault is reported for a session, the session is guaranteed to terminate. 
        /// This means any pending responses to the client, with the exception of the fault, will be canceled.
        /// </remarks>
        public Task<TFault> OnFault<TFault>()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Closes this instance.
        /// </summary>
        /// <returns>A task which when waited on indicates the completed close status of the transport.</returns>
        public Task Close()
        {
            // Close the given client.
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
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
        protected void ReadMessages()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Writes the outgoing messages to the remote stream.
        /// </summary>
        protected void WriteMessages()
        {
            throw new NotImplementedException();
        }
    }
}
