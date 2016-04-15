//-----------------------------------------------------------------------
// <copyright file="QueuedChunk.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    internal class QueuedChunk : IDisposable
    {
        private TaskCompletionSource<bool> completeTask = new TaskCompletionSource<bool>();
        private MemoryStream dataStream = new MemoryStream();
        private bool disposed = false;

        ~QueuedChunk()
        {
            this.Dispose(false);
        }

        internal MemoryStream DataStream
        {
            get
            {
                return this.dataStream;
            }
        }

        protected TaskCompletionSource<bool> CompleteTask
        {
            get
            {
                return this.completeTask;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal void Complete()
        {
            this.completeTask.TrySetResult(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                this.disposed = true;

                if (disposing)
                {
                    if (this.completeTask != null)
                    {
                        this.completeTask.TrySetCanceled();
                    }

                    if (this.dataStream != null)
                    {
                        this.dataStream.Dispose();
                        this.dataStream = null;
                    }
                }
            }
        }
    }
}
