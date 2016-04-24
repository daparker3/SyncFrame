//-----------------------------------------------------------------------
// <copyright file="MultiplexedStream.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame.Channels
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Threading;

    internal class MultiplexedStream : Stream
    {
        private MultiplexedStreamFactory factory;
        private int channelId;
        private AutoResetEvent readPendingEvent = new AutoResetEvent(false);
        private AutoResetEvent writeCompleteEvent = new AutoResetEvent(false);
        private object bufferLock = new object();
        private Queue<byte> readBuffer = new Queue<byte>();
        private int bufferCount = 0;
        private bool disposed = false;

        internal MultiplexedStream(MultiplexedStreamFactory factory, int channelId)
        {
            Contract.Requires(factory != null);
            Contract.Requires(channelId >= 0);
            this.factory = factory;
            this.channelId = channelId;
        }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports reading.
        /// </summary>
        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports seeking.
        /// </summary>
        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports writing.
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// When overridden in a derived class, gets the length in bytes of the stream.
        /// </summary>
        /// <exception cref="System.NotSupportedException">This member is not supported.</exception>
        public override long Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// When overridden in a derived class, gets or sets the position within the current stream.
        /// </summary>
        /// <exception cref="System.NotSupportedException">This member is not supported.</exception>
        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// When overridden in a derived class, clears all buffers for this stream and causes any buffered data to be written to the underlying device.
        /// </summary>
        public override void Flush()
        {
        }

        /// <summary>
        /// When overridden in a derived class, sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the <paramref name="origin" /> parameter.</param>
        /// <param name="origin">A value of type <see cref="T:System.IO.SeekOrigin" /> indicating the reference point used to obtain the new position.</param>
        /// <returns>
        /// The new position within the current stream.
        /// </returns>
        /// <exception cref="System.NotSupportedException">This member is not supported.</exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// When overridden in a derived class, sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        /// <exception cref="System.NotSupportedException">This member is not supported.</exception>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// When overridden in a derived class, reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset" /> and (<paramref name="offset" /> + <paramref name="count" /> - 1) replaced by the bytes read from the current source.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer" /> at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>
        /// The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.
        /// </returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            Contract.Requires(buffer != null);
            Contract.Requires(offset >= 0);
            Contract.Requires(count < buffer.Length);
            while (this.readBuffer.Count < count)
            {
                this.readPendingEvent.WaitOne();
            }

            lock (this.bufferLock)
            {
                for (int i = offset; i < offset + count; ++i)
                {
                    buffer[i] = this.readBuffer.Dequeue();
                }
            }

            return count;
        }

        /// <summary>
        /// When overridden in a derived class, writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies <paramref name="count" /> bytes from <paramref name="buffer" /> to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer" /> at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            Contract.Requires(buffer != null);
            Contract.Requires(offset >= 0);
            Contract.Requires(count < buffer.Length);
            PendingWriteChunk writeChunk = new PendingWriteChunk(this.writeCompleteEvent, this.channelId, buffer, offset, count);
            this.factory.BufferData(writeChunk);
            while (!writeChunk.Complete)
            {
                this.writeCompleteEvent.WaitOne();
            }
        }

        internal void BufferData(byte[] buffer, int offset, int count)
        {
            lock (this.bufferLock)
            {
                for (int i = offset; i < offset + count; ++i)
                {
                    this.readBuffer.Enqueue(buffer[i]);
                }
            }

            this.bufferCount += count;
            this.readPendingEvent.Set();
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.IO.Stream" /> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!this.disposed)
            {
                this.disposed = true;
                if (disposing)
                {
                    if (this.readPendingEvent != null)
                    {
                        this.readPendingEvent.Dispose();
                        this.readPendingEvent = null;
                    }

                    if (this.writeCompleteEvent != null)
                    {
                        this.writeCompleteEvent.Dispose();
                        this.writeCompleteEvent = null;
                    }
                }
            }
        }
    }
}
