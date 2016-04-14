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
        private ConcurrentDictionary<Type, ChunkCollection> pendingResponsesByType = new ConcurrentDictionary<Type, ChunkCollection>();
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
                foreach (ChunkCollection chunkBag in this.pendingResponsesByType.Values)
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

        internal async Task QueueResponse(Type responseType, QueuedResponseChunk qrc, CancellationToken responseCanceledToken)
        {
            ChunkCollection chunkBag = await this.GetChunkBag(responseType, responseCanceledToken);
            int responseSize = Marshal.SizeOf(typeof(QueuedResponseChunk)) + Marshal.SizeOf(responseType);
            this.ReleaseBuffer(responseSize);
            chunkBag.QueueChunk(qrc);
        }

        internal async Task<QueuedResponseChunk> DequeueResponse(Type responseType, CancellationToken responseCanceledToken)
        {
            ChunkCollection chunkBag = await this.GetChunkBag(responseType, responseCanceledToken);
            int responseSize = Marshal.SizeOf(typeof(QueuedResponseChunk)) + Marshal.SizeOf(responseType);
            await this.ReserveBuffer(responseSize, responseCanceledToken);
            return await chunkBag.DequeueChunk(responseCanceledToken);
        }

        internal void CancelResponses()
        {
            int canceled;
            do
            {
                canceled = 0;
                foreach (Type type in this.pendingResponsesByType.Keys)
                {
                    ChunkCollection chunkBag;
                    if (this.pendingResponsesByType.TryGetValue(type, out chunkBag))
                    {
                        canceled += chunkBag.CancelResponses();
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

                    foreach (ChunkCollection chunkBag in this.pendingResponsesByType.Values)
                    {
                        chunkBag.Dispose();
                    }
                }
            }
        }

        private async Task<ChunkCollection> GetChunkBag(Type responseType, CancellationToken responseCanceledToken)
        {
            ChunkCollection chunkBag;
            if (!this.pendingResponsesByType.TryGetValue(responseType, out chunkBag))
            {
                await this.ReserveBuffer(SizeOfChunkBag, responseCanceledToken);
                chunkBag = new ChunkCollection();
                if (!this.pendingResponsesByType.TryAdd(responseType, chunkBag))
                {
                    chunkBag.Dispose();
                    chunkBag = this.pendingResponsesByType[responseType];
                }
            }

            return chunkBag;
        }

        private async Task ReserveBuffer(int bufSz, CancellationToken responseCanceledToken)
        {
            if (bufSz > this.BufferSize)
            {
                throw new InvalidOperationException();
            }

            this.BufferUse += bufSz;
            while (this.BufferUse > this.BufferSize)
            {
                await this.responseCompleteEvent.GetTaskSignalingCompletion();
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

        private class ChunkCollection : IDisposable
        {
            private ConcurrentBag<QueuedResponseChunk> chunkBag = new ConcurrentBag<QueuedResponseChunk>();
            private AutoResetEvent chunkQueuedEvent = new AutoResetEvent(false);
            private bool disposed = false;

            ~ChunkCollection()
            {
                this.Dispose(false);
            }

            internal int Count
            {
                get
                {
                    return this.chunkBag.Count;
                }
            }

            public void Dispose()
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }

            internal void QueueChunk(QueuedResponseChunk chunk)
            {
                this.chunkBag.Add(chunk);
                this.chunkQueuedEvent.Set();
            }

            internal async Task<QueuedResponseChunk> DequeueChunk(CancellationToken token)
            {
                QueuedResponseChunk ret;
                while (!this.chunkBag.TryTake(out ret))
                {
                    await this.chunkQueuedEvent.GetTaskSignalingCompletion();
                }

                return ret;
            }

            internal int CancelResponses()
            {
                int canceled = 0;
                do
                {
                    canceled = 0;
                    QueuedResponseChunk response;
                    while (this.chunkBag.TryTake(out response))
                    {
                        response.RequestCompleteTask.TrySetCanceled();
                        response.Dispose();
                        ++canceled;
                    }
                }
                while (canceled > 0);
                return canceled;
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!this.disposed)
                {
                    this.disposed = true;

                    if (disposing)
                    {
                        if (this.chunkQueuedEvent != null)
                        {
                            this.chunkQueuedEvent.Dispose();
                            this.chunkQueuedEvent = null;
                        }

                        QueuedResponseChunk response;
                        while (this.chunkBag.TryTake(out response))
                        {
                            response.Dispose();
                        }
                    }
                }
            }
        }
    }
}
