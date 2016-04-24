//-----------------------------------------------------------------------
// <copyright file="MultiplexedStreamFactory.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame.Channels
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using EnsureThat;
    using Properties;
    using ProtoBuf;

    /// <summary>
    /// The multiplexed stream factory lets you create multiple <see cref="Stream"/> objects over a single network connection. 
    /// This allows you to share a single connection between multiple <see cref="MessageTransport"/> objects.
    /// Each channel must share a unique <see cref="Int32"/> identifier on both sides of the connection. Once a factory is instantiated,
    /// the underlying stream can't be shared with other <see cref="MultiplexedStreamFactory"/> objects.
    /// </summary>
    public class MultiplexedStreamFactory : IDisposable
    {
        private Stream underlyingStream;
        private CancellationToken connectionClosedToken;
        private CancellationTokenRegistration connectionClosedTokenRegistration;
        private ConcurrentDictionary<int, MultiplexedStream> streamsByChannel = new ConcurrentDictionary<int, MultiplexedStream>();
        private ConcurrentQueue<PendingWriteChunk> writeData = new ConcurrentQueue<PendingWriteChunk>();
        private AutoResetEvent writePendingEvent = new AutoResetEvent(false);
        private int readBufferSize = 1 << 12;
        private byte[] readBuffer;
        private bool disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiplexedStreamFactory"/> class.
        /// </summary>
        /// <param name="underlyingStream">The underlying stream.</param>
        public MultiplexedStreamFactory(Stream underlyingStream)
            : this(underlyingStream, CancellationToken.None)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiplexedStreamFactory"/> class.
        /// </summary>
        /// <param name="underlyingStream">The underlying stream.</param>
        /// <param name="token">An optional <see cref="CancellationToken"/> which can be used to close the session.</param>
        public MultiplexedStreamFactory(Stream underlyingStream, CancellationToken token)
        {
            Ensure.That(underlyingStream, "underlyingStream").IsNotNull();
            this.underlyingStream = underlyingStream;
            this.connectionClosedToken = token;
            this.readBuffer = new byte[this.readBufferSize];
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="MultiplexedStreamFactory"/> class.
        /// </summary>
        ~MultiplexedStreamFactory()
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
        /// Gets or sets the size of the read buffer in bytes.
        /// </summary>
        /// <value>
        /// The size of the read buffer.
        /// </value>
        /// <remarks>
        /// The read buffer is used as temporary storage when copying messages from the network stream into their response storage.
        /// A default value of 4096 bytes is used.
        /// </remarks>
        public int ReadBufferSize
        {
            get
            {
                return this.readBufferSize;
            }

            set
            {
                Ensure.That(value, "value").IsGt(0);
                this.readBufferSize = value;
                this.readBuffer = new byte[this.readBufferSize];
            }
        }

        internal CancellationToken ConnectionClosedToken
        {
            get
            {
                return this.connectionClosedToken;
            }
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
        /// Opens the connection and begins multiplexing data.
        /// </summary>
        /// <returns>A task which when complete indicates the transport has completed the session.</returns>
        /// <exception cref="OperationCanceledException">Occurs if the session was canceled.</exception>
        public async Task Open()
        {
            Contract.Requires(!this.IsConnectionOpen, Resources.ConnectionAlreadyOpened);
            this.IsConnectionOpen = true;
            this.connectionClosedTokenRegistration = this.connectionClosedToken.Register(this.ConnectionClosedHandler);

            // Needed, because the call to DeserializeWithLengthPrefix may block.
            Task readTask = Task.Run(async () => await this.ReadMessages());
            Task writeTask = this.WriteMessages();
            while (this.IsConnectionOpen)
            {
                await Task.Factory.ContinueWhenAny(
                    new Task[] { readTask, writeTask },
                    (t) =>
                    {
                        this.connectionClosedToken.ThrowIfCancellationRequested();
                        if (t == readTask)
                        {
                            // Needed, because the call to DeserializeWithLengthPrefix may block.
                            readTask = Task.Run(async () => await this.ReadMessages());
                        }
                        else if (t == writeTask)
                        {
                            writeTask = this.WriteMessages();
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    },
                    this.connectionClosedToken);
            }
        }

        /// <summary>
        /// Creates the channel.
        /// </summary>
        /// <param name="channelId">The channel identifier.</param>
        /// <returns>A stream enabling communication for the specified channel identifier.</returns>
        public Stream CreateChannel(int channelId)
        {
            Ensure.That(channelId, "channelId").IsGte(0);
            return this.GetChannelStream(channelId);
        }

        internal void BufferData(PendingWriteChunk writeChunk)
        {
            Contract.Requires(writeChunk != null);
            this.writeData.Enqueue(writeChunk);
            this.writePendingEvent.Set();
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            Contract.Ensures(this.disposed == true);
            if (!this.disposed)
            {
                this.disposed = true;

                if (disposing)
                {
                    if (this.underlyingStream != null)
                    {
                        this.underlyingStream.Close();
                        this.underlyingStream.Dispose();
                        this.underlyingStream = null;
                    }

                    foreach (int channelId in this.streamsByChannel.Keys)
                    {
                        MultiplexedStream readStream;
                        if (this.streamsByChannel.TryRemove(channelId, out readStream))
                        {
                            readStream.Close();
                            readStream.Dispose();
                        }
                    }

                    this.streamsByChannel = null;

                    if (this.writePendingEvent != null)
                    {
                        this.writePendingEvent.Dispose();
                        this.writePendingEvent = null;
                    }
                }
            }
        }

        private async Task ReadMessages()
        {
            MultiplexedDataHeader header = Serializer.DeserializeWithLengthPrefix<MultiplexedDataHeader>(this.underlyingStream, PrefixStyle.Base128);
            MultiplexedStream channelStream = this.GetChannelStream(header.ChannelId);
            int remaining = header.Length;
            while (remaining > 0)
            {
                int toCopy = remaining;
                if (toCopy > this.readBuffer.Length)
                {
                    toCopy = this.readBuffer.Length;
                }

                await this.underlyingStream.ReadAsync(this.readBuffer, 0, toCopy, this.connectionClosedToken);
                channelStream.BufferData(this.readBuffer, 0, toCopy);
                remaining -= toCopy;
            }
        }

        private async Task WriteMessages()
        {
            await this.writePendingEvent.GetTaskSignalingCompletion();
            PendingWriteChunk data;
            while (this.writeData.TryDequeue(out data))
            {
                Serializer.SerializeWithLengthPrefix(this.underlyingStream, data.Header, PrefixStyle.Base128);
                await this.underlyingStream.WriteAsync(data.QueuedData.Array, data.QueuedData.Offset, data.QueuedData.Count, this.connectionClosedToken);
                data.WriteComplete();
            }
        }

        private MultiplexedStream GetChannelStream(int channelId)
        {
            MultiplexedStream channelStream;
            if (!this.streamsByChannel.TryGetValue(channelId, out channelStream))
            {
                channelStream = new MultiplexedStream(this, channelId);
                if (!this.streamsByChannel.TryAdd(channelId, channelStream))
                {
                    channelStream.Dispose();
                    channelStream = this.streamsByChannel[channelId];
                }
            }

            return channelStream;
        }

        private void ConnectionClosedHandler()
        {
            this.IsConnectionOpen = false;
            if (this.underlyingStream != null)
            {
                this.underlyingStream.Close();
            }

            foreach (MultiplexedStream stream in this.streamsByChannel.Values)
            {
                stream.Close();
            }
        }
    }
}
