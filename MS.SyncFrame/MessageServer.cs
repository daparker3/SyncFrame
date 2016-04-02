//-----------------------------------------------------------------------
// <copyright file="MessageServer.cs" company="MS">
//     Copyright (c) 2016 MS.
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// A message server represents a responder in a client-server messaging session.
    /// </summary>
    /// <seealso cref="MS.SyncFrame.MessageTransport" />
    /// <example>
    /// The following example shows a simple message server which reads and writes random fill data.
    /// <code language="c#" title="Creating a simple MessageServer"><![CDATA[
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
    ///     internal static Task CreateListenTask(Stream clientStream, int responseSize)
    ///     {
    ///         return Task.Factory.StartNew(async () =>
    ///         {
    ///             Random r = new Random();
    ///             MessageServer server = await MessageServer.CreateServerSession(clientStream);
    ///             await server.Listen();
    /// 
    ///             while (server.IsConnectionOpen)
    ///             {
    ///                 await server.RecieveData<Message>()
    ///                             .ContinueWith((t) => server.SendData(new Message { Data = responseData }))
    ///                             .Unwrap();
    ///             }
    /// 
    ///             await server.Close();
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
    public class MessageServer : MessageTransport
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageServer"/> class.
        /// </summary>
        /// <param name="serverStream">The server stream.</param>
        private MessageServer(Stream serverStream)
            : base(serverStream)
        {
        }

        /// <summary>
        /// Creates the server session.
        /// </summary>
        /// <param name="serverStream">The client stream.</param>
        /// <returns>A task which when waited on, yields an active server session.</returns>
        /// <remarks>The <paramref name="serverStream"/> parameter is assumed to have the same properties as a stream derived from a call to <see cref="System.Net.Sockets.TcpClient.GetStream"/>. You must initialize the TCP server session yourself before attempting to initialize this object.</remarks>
        public static Task<MessageServer> CreateServerSession(Stream serverStream)
        {
            // Yield a new message client...
            throw new NotImplementedException();
        }

        /// <summary>
        /// Starts listening for messages.
        /// </summary>
        /// <returns>A task which when complete indicates the server has started listening for messages.</returns>
        public Task Listen()
        {
            throw new NotImplementedException();
        }
    }
}
