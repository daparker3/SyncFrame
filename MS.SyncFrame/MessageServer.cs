//-----------------------------------------------------------------------
// <copyright file="MessageServer.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Properties;

    /// <summary>
    /// A message server represents a responder in a client-server messaging session.
    /// </summary>
    /// <seealso cref="MS.SyncFrame.MessageTransport" />
    /// <example>
    /// The following example shows a simple message server which reads and writes random fill data.
    /// <code language="c#" title="Creating a simple MessageServer"><![CDATA[
    /// internal static Task CreateListenTask(Stream serverStream, int responseSize)
    /// {
    ///     return Task.Factory.StartNew(async () =>
    ///     {
    ///         Random r = new Random();
    ///         using (CancellationTokenSource cts = new CancellationTokenSource())
    ///         using (MessageServer server = new MessageServer(serverStream, cts.Token))
    ///         {
    ///             Task sessionTask = server.Open();
    /// 
    ///             while (server.IsConnectionOpen)
    ///             {
    ///                 byte[] responseData = new byte[responseSize];
    ///                 r.NextBytes(responseData);
    ///                 TypedResult<Message> request = await server.ReceiveData<Message>();
    ///                 await request.SendData(new Message { Data = responseData });
    ///             }
    /// 
    ///             cts.Cancel();
    ///             await sessionTask;
    ///         }
    ///     });
    /// }
    /// ]]></code>
    /// </example>
    /// <remarks>
    /// When an exception is listed in one of the documentation nodes for this class, it is possible that the exception
    /// will be thrown by itself, or inside an <see cref="AggregateException"/>.
    /// </remarks>
    public class MessageServer : MessageTransport
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageServer"/> class.
        /// </summary>
        /// <param name="serverStream">The server stream.</param>
        /// <param name="token">The cancellation token.</param>
        public MessageServer(Stream serverStream, CancellationToken token)
            : base(serverStream, token)
        {
        }

        /// <summary>
        /// Opens the connection and begins listening for messages.
        /// </summary>
        /// <returns>
        /// A task which when complete indicates the transport has completed the session.
        /// </returns>
        /// <remarks>
        /// This can be overridden in child classes to provide additional open behavior.
        /// </remarks>
        /// <exception cref="ConnectionClosedException">Occurs if the connection was closed.</exception>
        public override async Task Open()
        {
            try
            {
                await base.Open();

                while (this.IsConnectionOpen)
                {
                    await this.ReadMessages();
                    await this.WriteMessages();
                }
            }
            catch (Exception ex)
            {
                throw new ConnectionClosedException(Resources.ConnectionClosed, ex);
            }
            finally
            {
                try
                {
                    this.Dispose();
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
