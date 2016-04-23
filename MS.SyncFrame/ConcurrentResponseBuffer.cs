//-----------------------------------------------------------------------
// <copyright file="ConcurrentResponseBuffer.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics.Contracts;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using EnsureThat;

    internal class ConcurrentResponseBuffer : IDisposable
    {
        private ConcurrentDictionary<Type, ChunkCollection> pendingResponsesByType = new ConcurrentDictionary<Type, ChunkCollection>();
        private int bufferSize;
        private AutoResetEvent responseCompleteEvent = new AutoResetEvent(false);
        private bool canceling = false;
        private bool disposed = false;

        internal ConcurrentResponseBuffer()
        {
            this.BufferUse = 0;
            this.BufferSize = int.MaxValue;
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

        internal int BufferUse
        {
            get;
            private set;
        }

        internal int BufferSize
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
            Contract.Requires(responseType != null);
            Contract.Requires(qrc != null);
            Contract.Requires(qrc.DataStream.Length == (int)qrc.DataStream.Length);
            ChunkCollection chunkBag = this.GetChunkBag(responseType, responseCanceledToken, true);
            await this.ReserveBuffer((int)qrc.DataStream.Length, responseCanceledToken);
            chunkBag.QueueChunk(qrc);
        }

        internal async Task<QueuedResponseChunk> DequeueResponse(Type responseType, CancellationToken responseCanceledToken)
        {
            Contract.Requires(responseType != null);
            ChunkCollection chunkBag = this.GetChunkBag(responseType, responseCanceledToken, true);
            QueuedResponseChunk chunk = await chunkBag.DequeueChunk(responseCanceledToken);
            Contract.Assert(chunk != null);
            if (chunk != null)
            {
                this.ReleaseBuffer((int)chunk.DataStream.Length);
            }

            return chunk;
        }

        internal void CancelResponses()
        {
            Contract.Ensures(this.pendingResponsesByType.Count == 0);
            this.canceling = true;
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
            Contract.Ensures(this.disposed == true);
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

        private ChunkCollection GetChunkBag(Type responseType, CancellationToken responseCanceledToken, bool createIfNotExist)
        {
            Contract.Requires(responseType != null);
            ChunkCollection chunkBag;
            if (!this.pendingResponsesByType.TryGetValue(responseType, out chunkBag))
            {
                if (createIfNotExist)
                {
                    chunkBag = new ChunkCollection();
                    if (!this.pendingResponsesByType.TryAdd(responseType, chunkBag))
                    {
                        chunkBag.Dispose();
                        chunkBag = this.pendingResponsesByType[responseType];
                    }
                }
            }

            return chunkBag;
        }

        private async Task ReserveBuffer(int bufSz, CancellationToken responseCanceledToken)
        {
            Contract.Requires(bufSz > 0);
            Contract.Requires(bufSz <= this.BufferSize);
            Contract.Ensures(this.BufferUse <= this.BufferSize);
            this.BufferUse += bufSz;
            while (!this.canceling && this.BufferUse > this.BufferSize)
            {
                await this.responseCompleteEvent.GetTaskSignalingCompletion();
            }
        }

        private void ReleaseBuffer(int bufSz)
        {
            Contract.Requires(bufSz > 0);
            Contract.Requires(bufSz <= this.BufferSize);
            Contract.Ensures(this.BufferUse >= 0);
            this.BufferUse -= bufSz;
            this.responseCompleteEvent.Set();
        }

        private class ChunkCollection : IDisposable
        {
            private ConcurrentBag<QueuedResponseChunk> chunkBag = new ConcurrentBag<QueuedResponseChunk>();
            private AutoResetEvent chunkQueuedEvent = new AutoResetEvent(false);
            private bool disposed = false;
            private bool canceling = false;

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
                Contract.Requires(chunk != null);
                this.chunkBag.Add(chunk);
                this.chunkQueuedEvent.Set();
            }

            internal async Task<QueuedResponseChunk> DequeueChunk(CancellationToken token)
            {
                QueuedResponseChunk ret = null;
                while (!this.canceling && !this.chunkBag.TryTake(out ret))
                {
                    await this.chunkQueuedEvent.GetTaskSignalingCompletion();
                }

                return ret;
            }

            internal int CancelResponses()
            {
                Contract.Ensures(this.chunkBag.Count == 0);
                this.canceling = true;
                int canceled = 0;
                do
                {
                    canceled = 0;
                    QueuedResponseChunk response;
                    while (this.chunkBag.TryTake(out response))
                    {
                        response.Dispose();
                        ++canceled;
                    }
                }
                while (canceled > 0);
                return canceled;
            }

            protected virtual void Dispose(bool disposing)
            {
                Contract.Ensures(this.disposed == true);
                if (!this.disposed)
                {
                    this.disposed = true;

                    if (disposing)
                    {
                        QueuedResponseChunk response;
                        while (this.chunkBag.TryTake(out response))
                        {
                            response.Dispose();
                        }

                        this.chunkQueuedEvent.Dispose();
                    }
                }
            }
        }
    }
}
