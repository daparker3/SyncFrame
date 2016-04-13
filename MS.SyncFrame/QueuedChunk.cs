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
        private bool disposed = false;

        internal QueuedChunk()
        {
            this.RequestCompleteTask = new TaskCompletionSource<bool>();
            this.DataStream = new MemoryStream();
        }

        ~QueuedChunk()
        {
            this.Dispose(false);
        }

        internal MemoryStream DataStream { get; private set; }

        internal TaskCompletionSource<bool> RequestCompleteTask { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                this.disposed = true;

                if (disposing)
                {
                    if (this.DataStream != null)
                    {
                        this.DataStream.Dispose();
                        this.DataStream = null;
                    }
                }
            }
        }

        private void Reset()
        {
            this.RequestCompleteTask.TrySetCanceled();
            this.RequestCompleteTask = new TaskCompletionSource<bool>();
            if (this.DataStream != null)
            {
                this.DataStream.Dispose();
            }

            this.DataStream = new MemoryStream();
        }
    }
}
