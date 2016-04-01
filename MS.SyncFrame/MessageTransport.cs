//-----------------------------------------------------------------------
// <copyright file="MessageTransport.cs" company="MS">
//     Copyright (c) 2016 MS.
// </copyright>
//-----------------------------------------------------------------------
namespace MS.SyncFrame
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// A message transport represents the base messaging functionality for a client or server.
    /// </summary>
    public class MessageTransport
    {
        /// <summary>
        /// Gets or sets the maximum size of the frame.
        /// </summary>
        /// <value>
        /// The maximum transmitted size of a sync frame before fragmentation occurs in bytes.
        /// </value>
        public int MaxFrameSize
        {
            get;
            set;
        }

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        /// <returns>A task which will waited on will yield the response.</returns>
        public Task<TResponse> SendMessage<TResponse, TMessage>(TMessage message)
        {
            // Send a message of the type TMessage to the server, 
            // waiting for a response of type TResponse.
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        /// <returns>A task which when waited on will complete when the message is handled remotely.</returns>
        public Task SendMessage<TMessage>(TMessage message)
        {
            // Send a message of the type TMessage to the server, 
            // completing the task when the message is sent.
            throw new NotImplementedException();
        }

        /// <summary>
        /// Receives the response.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <returns>A task which will waited on will yield the response.</returns>
        public Task<TResponse> RecieveResponse<TResponse>()
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
    }
}
