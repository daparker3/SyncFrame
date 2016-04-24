//-----------------------------------------------------------------------
// <copyright file="PendingWriteChunk.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame.Channels
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Threading;

    internal class PendingWriteChunk
    {
        private AutoResetEvent writeCompleteEvent;

        internal PendingWriteChunk(AutoResetEvent writeCompleteEvent, int channelId, byte[] data, int offset, int length)
        {
            Contract.Requires(writeCompleteEvent != null);
            Contract.Requires(channelId >= 0);
            Contract.Requires(data != null);
            Contract.Requires(offset >= 0);
            Contract.Requires(length <= data.Length);
            this.Header = new MultiplexedDataHeader { ChannelId = channelId, Length = length };
            this.QueuedData = new ArraySegment<byte>(data, offset, length);
            this.Complete = false;
            this.writeCompleteEvent = writeCompleteEvent;
        }

        internal MultiplexedDataHeader Header { get; private set; }

        internal ArraySegment<byte> QueuedData { get; private set; }

        internal bool Complete { get; private set; }

        internal void WriteComplete()
        {
            this.Complete = true;
            this.writeCompleteEvent.Set();
        }
    }
}
