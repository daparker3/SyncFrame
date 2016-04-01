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
    public class MessageServer : MessageTransport
    {
        /// <summary>
        /// Creates the server session.
        /// </summary>
        /// <param name="serverStream">The client stream.</param>
        /// <returns>A task which when waited on, yields an active server session.</returns>
        /// <remarks>The <paramref name="serverStream"/> parameter is assumed to have the same properties as a stream derived from a call to <see cref="System.Net.Sockets.TcpClient.GetStream"/>. You must initialize the TCP server session yourself before attempting to initialize this object.</remarks>
        public static Task<MessageClient> CreateServertSession(Stream serverStream)
        {
            // Yield a new message client...
            throw new NotImplementedException();
        }
    }
}
