//-----------------------------------------------------------------------
// <copyright file="QueuedRequestChunk.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    internal sealed class QueuedRequestChunk : IDisposable
    {
        internal QueuedRequestChunk(int capacity)
        {
            this.RequestCompleteTask = new TaskCompletionSource<int>();
            this.DataStream = new MemoryStream(capacity);
        }

        internal MemoryStream DataStream { get; private set; }

        internal TaskCompletionSource<int> RequestCompleteTask { get; private set; }

        public void Dispose()
        {
            if (this.DataStream != null)
            {
                this.DataStream.Dispose();
                this.DataStream = null;
            }
        }
    }
}
