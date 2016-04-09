//-----------------------------------------------------------------------
// <copyright file="MessageClient.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------
namespace MS.SyncFrame
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A message client represents the requester in a client-server messaging session.
    /// </summary>
    /// <seealso cref="MS.SyncFrame.MessageTransport" />
    /// <example>
    /// The following example shows a simple message client which reads and writes random fill data.
    /// <code language="c#" title="Creating a simple MessageClient"><![CDATA[
    /// internal static Task CreateTransmitTask(NetworkStream clientStream, TimeSpan frameDelay, int numRequests, int requestSize)
    /// {
    ///     return Task.Factory.StartNew(async () =>
    ///     {
    ///         Random r = new Random();
    ///         using (CancellationTokenSource cts = new CancellationTokenSource())
    ///         using (MessageClient client = new MessageClient(clientStream, cts.Token))
    ///         {
    ///             client.MinDelay = frameDelay;
    ///             Task sessionTask = client.Open();
    /// 
    ///             for (int i = 0; i < numRequests; ++i)
    ///             {
    ///                 // Do something with the message.
    ///                 byte[] requestData = new byte[requestSize];
    ///                 r.NextBytes(requestData);
    ///                 Message requestMessage = new Message { Data = requestData };
    ///                 TypedResult<Message> responseMessage = await client.SendData(requestMessage)
    ///                                                                     .ReceiveData<Message>();
    ///             }
    /// 
    ///             cts.Cancel();
    ///             await sessionTask;
    ///         }
    ///     });
    /// }
    /// ]]></code>
    /// </example>
    public class MessageClient : MessageTransport
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageClient"/> class.
        /// </summary>
        /// <param name="clientStream">The client stream.</param>
        /// <param name="token">The cancellation token.</param>
        public MessageClient(Stream clientStream, CancellationToken token)
            : base(clientStream, token)
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
        /// Opens the connection and begins listening for messages.
        /// </summary>
        /// <returns>
        /// A task which when complete indicates the transport has completed the session.
        /// </returns>
        /// <remarks>
        /// This can be overridden in child classes to provide additional open behavior.
        /// </remarks>
        public override Task Open()
        {
            return base.Open().ContinueWith(async (t) =>
            {
                while (this.IsConnectionOpen)
                {
                    Task delayTask = Task.Delay(this.MinDelay, this.ConnectionClosedToken);
                    await this.WriteMessages();
                    await this.ReadMessages();
                    await delayTask;
                }
            });
        }
    }
}
