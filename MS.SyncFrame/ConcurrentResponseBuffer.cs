//-----------------------------------------------------------------------
// <copyright file="ConcurrentResponseBuffer.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using EnsureThat;

    internal class ConcurrentResponseBuffer : IDisposable
    {
        private static readonly int SizeOfChunkBag = Marshal.SizeOf(typeof(ConcurrentBag<QueuedResponseChunk>));
        private Random rand = new Random();
        private ConcurrentDictionary<Type, ConcurrentBag<QueuedResponseChunk>> pendingResponsesByType = new ConcurrentDictionary<Type, ConcurrentBag<QueuedResponseChunk>>();
        private long bufferSize;
        private AutoResetEvent responseCompleteEvent = new AutoResetEvent(false);
        private bool disposed = false;

        internal ConcurrentResponseBuffer()
        {
            this.BufferUse = 0;
            this.BufferSize = long.MaxValue;
        }

        ~ConcurrentResponseBuffer()
        {
            this.Dispose(false);
        }

        internal int Count
        {
            get
            {
                int count = 0;
                foreach (ConcurrentBag<QueuedResponseChunk> chunkBag in this.pendingResponsesByType.Values)
                {
                    count += chunkBag.Count;
                }

                return count;
            }
        }

        internal long BufferUse
        {
            get;
            private set;
        }

        internal long BufferSize
        {
            get
            {
                return this.bufferSize;
            }

            set
            {
                Ensure.That(value, "value").IsGte(0);
                this.bufferSize = value;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal async Task<QueuedResponseChunk> TakeNextResponse(Type responseType, CancellationToken responseCanceledToken)
        {
            ConcurrentBag<QueuedResponseChunk> chunkBag;
            if (!this.pendingResponsesByType.TryGetValue(responseType, out chunkBag))
            {
                await this.ReserveBuffer(SizeOfChunkBag, responseCanceledToken);
                this.pendingResponsesByType.TryAdd(responseType, new ConcurrentBag<QueuedResponseChunk>());
                chunkBag = this.pendingResponsesByType[responseType];
            }

            QueuedResponseChunk qrc;
            int responseSize = Marshal.SizeOf(typeof(QueuedResponseChunk)) + Marshal.SizeOf(responseType);
            if (chunkBag.TryTake(out qrc))
            {
                this.ReleaseBuffer(responseSize);
            }
            else
            {
                await this.ReserveBuffer(responseSize, responseCanceledToken);
                qrc = new QueuedResponseChunk();
                chunkBag.Add(qrc);
            }

            return qrc;
        }

        internal void CancelResponses()
        {
            int canceled;
            do
            {
                canceled = 0;
                foreach (Type type in this.pendingResponsesByType.Keys)
                {
                    ConcurrentBag<QueuedResponseChunk> chunkBag;
                    if (this.pendingResponsesByType.TryGetValue(type, out chunkBag))
                    {
                        QueuedResponseChunk response;
                        while (chunkBag.TryTake(out response))
                        {
                            response.RequestCompleteTask.TrySetCanceled();
                            response.Dispose();
                            ++canceled;
                        }
                    }
                }
            }
            while (canceled > 0);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                this.disposed = true;

                if (disposing)
                {
                    if (this.responseCompleteEvent != null)
                    {
                        this.responseCompleteEvent.Dispose();
                        this.responseCompleteEvent = null;
                    }
                }
            }
        }

        private void ReleaseBuffer(int bufSz)
        {
            this.BufferUse -= bufSz;
            if (this.BufferUse < 0)
            {
                throw new InvalidOperationException();
            }

            this.responseCompleteEvent.Set();
        }

        private async Task ReserveBuffer(int bufSz, CancellationToken responseCanceledToken)
        {
            if (bufSz > this.BufferSize)
            {
                throw new InvalidOperationException();
            }

            this.BufferUse += bufSz;
            while (this.BufferUse >= this.BufferSize)
            {
                TaskCompletionSource<bool> waitedTcs = new TaskCompletionSource<bool>();
                RegisteredWaitHandle rwh = ThreadPool.RegisterWaitForSingleObject(this.responseCompleteEvent, (o, e) => waitedTcs.SetResult(true), null, -1, true);
                try
                {
                    await waitedTcs.Task;
                }
                finally
                {
                    rwh.Unregister(this.responseCompleteEvent);
                }
            }
        }
    }
}
