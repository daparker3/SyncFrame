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
    public class MessageClient : MessageTransport
    {
        /// <summary>
        /// Gets or sets the minimum delay between transmitting sync frames.
        /// </summary>
        /// <value>
        /// The minimum delay.
        /// </value>
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
