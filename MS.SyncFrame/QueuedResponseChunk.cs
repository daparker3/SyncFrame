//-----------------------------------------------------------------------
// <copyright file="QueuedResponseChunk.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Threading.Tasks;

    internal class QueuedResponseChunk : QueuedChunk
    {
        private TaskCompletionSource<bool> completeTask = new TaskCompletionSource<bool>();
        private MessageHeader header;
        private bool disposed = false;

        internal QueuedResponseChunk(Stream dataStream)
            : base(dataStream)
        {
            Contract.Requires(dataStream != null);
        }

        internal TaskCompletionSource<bool> CompleteTask
        {
            get
            {
                return this.completeTask;
            }
        }

        internal MessageHeader Header
        {
            get
            {
                return this.header;
            }

            set
            {
                Contract.Requires(value != null);
                this.header = value;
            }
        }

        internal virtual async Task ResponseComplete()
        {
            bool result = await this.CompleteTask.Task;
            Contract.Assert(result == true);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!this.disposed)
            {
                this.disposed = true;
                if (disposing)
                {
                    if (this.completeTask != null)
                    {
                        this.completeTask.TrySetCanceled();
                    }
                }
            }
        }
    }
}