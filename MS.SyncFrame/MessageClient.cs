//-----------------------------------------------------------------------
// <copyright file="MessageClient.cs" company="MS">
//     Copyright (c) 2016 MS.
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// A message client represents the requester in a client-server messaging session.
    /// </summary>
    /// <seealso cref="MS.SyncFrame.MessageTransport" />
    /// <example>
    /// The following example shows a simple message client which reads and writes random fill data.
    /// <code language="c#" title="Creating a simple MessageClient"><![CDATA[
    /// using System;
    /// using System.Collections.Generic;
    /// using System.IO;
    /// using System.Net.Sockets;
    /// using System.Threading.Tasks;
    /// using MS.SyncFrame;
    /// using ProtoBuf;
    ///
    /// internal static class ClientServer
    /// {
    ///     internal static Task CreateTransmitTask(NetworkStream serverStream, TimeSpan frameDelay, int numRequests, int requestSize)
    ///     {
    ///         return Task.Factory.StartNew(async () =>
    ///         {
    ///             Random r = new Random();
    ///             MessageClient client = await MessageClient.CreateClientSession(serverStream);
    ///             client.MinDelay = frameDelay;
    /// 
    ///             for (int i = 0; i < numRequests; ++i)
    ///             {
    ///                 // Do something with the message.
    ///                 byte[] requestData = new byte[requestSize];
    ///                 r.NextBytes(requestData);
    ///                 Message requestMessage = new Message { Data = requestData };
    ///                 Message responseMessage = client.SendData(requestMessage)
    ///                                                 .ContinueWith((t) => client.RecieveData<Message>())
    ///                                                 .Unwrap().Result;
    ///             }
    ///         });
    ///     }
    ///
    ///     [ProtoContract]
    ///     internal class Message
    ///     {
    ///         [ProtoMember(1)]
    ///         internal byte[] Data { get; set; }
    ///     }
    /// }]]></code>
    /// </example>
    public class MessageClient : MessageTransport
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageClient"/> class.
        /// </summary>
        /// <param name="clientStream">The client stream.</param>
        private MessageClient(Stream clientStream)
            : base(clientStream)
        {
            this.MinDelay = TimeSpan.FromMilliseconds(25);
        }

        /// <summary>
        /// Gets or sets the minimum delay between transmitting sync frames.
        /// </summary>
        /// <value>
        /// The minimum delay.
        /// </value>
        /// <remarks>The default minimum delay is a value of 25 milliseconds.</remarks>
        public TimeSpan MinDelay
        {
            get;
            set;
        }

        /// <summary>
        /// Creates the client session.
        /// </summary>
        /// <param name="clientStream">The client stream.</param>
        /// <returns>A task which when waited on, yields an active client session.</returns>
        /// <remarks>The <paramref name="clientStream"/> parameter is assumed to have the same properties as a stream derived from a call to <see cref="System.Net.Sockets.TcpClient.GetStream"/>. You must initialize the TCP client session yourself before attempting to initialize this object.</remarks>
        public static Task<MessageClient> CreateClientSession(Stream clientStream)
        {
            // Yield a new message client...
            throw new NotImplementedException();
        }
    }
}
