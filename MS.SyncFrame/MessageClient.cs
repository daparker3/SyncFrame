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
    using EnsureThat;

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
    ///                 Message responseMessage = await client.SendData(requestMessage)
    ///                                                       .ReceiveData<Message>()
    ///                                                       .Complete();
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
    public class MessageClient : MessageTransport
    {
        private TaskCompletionSource<TimeSpan> latencyGapMeasurementTcs = new TaskCompletionSource<TimeSpan>();
        private TimeSpan minDelay;

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
            get
            {
                return this.minDelay;
            }

            set
            {
                Ensure.That(value, "value").IsGte(TimeSpan.Zero);
                this.minDelay = value;
            }
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
        /// <exception cref="OperationCanceledException">Occurs if the session was canceled.</exception>
        /// <exception cref="FaultException{TFault}">Occurs if a response to a remote request generates a fault.</exception>
        public override async Task Open()
        {
            await base.Open();

            try
            {
                while (this.IsConnectionOpen)
                {
                    Task delayTask = Task.Delay(this.MinDelay, this.ConnectionClosedToken);
                    await this.WriteMessages();
                    await this.ReadMessages();
                    DateTime gapStart = DateTime.Now;
                    await delayTask;
                    this.latencyGapMeasurementTcs.SetResult(DateTime.Now - gapStart);
                    this.latencyGapMeasurementTcs = new TaskCompletionSource<TimeSpan>();
                }
            }
            catch (Exception)
            {
                this.latencyGapMeasurementTcs.TrySetCanceled();
                throw;
            }
        }

        /// <summary>
        /// Gets the latency span.
        /// </summary>
        /// <returns>
        /// A task which when completed results in a <see cref="TimeSpan"/> 
        /// that represents the difference between when the last sync frame completed and when the last delay completed.
        /// </returns>
        /// <remarks>
        /// The latency span is meant to be an instantaneous measure of what part of the delay in a sync interval isn't being used to transmit or receive data.
        /// Since several factors, including the quality of the connection itself, can wildly influence what this
        /// value will be, it's recommended that this value can be used as part of a gradual averaging strategy to tune the latency level of your application based on
        /// the quality of connection and volume of data over time. It's also recommended that you bound your latency under a maximum value representing the highest
        /// level of delay your application can tolerate.
        /// </remarks>
        public Task<TimeSpan> GetLatencySpan()
        {
            return this.latencyGapMeasurementTcs.Task;
        }
    }
}
