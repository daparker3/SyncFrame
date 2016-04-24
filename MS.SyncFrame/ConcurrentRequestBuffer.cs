//-----------------------------------------------------------------------
// <copyright file="ConcurrentRequestBuffer.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;

    internal class ConcurrentRequestBuffer
    {
        private const int SizeRequestChunkRetryBuckets = 10;
        private ConcurrentBag<QueuedRequestChunk>[] queuedRequestChunks = new ConcurrentBag<QueuedRequestChunk>[SizeRequestChunkRetryBuckets];
        private ConcurrentDictionary<Type, int> typeIdsByType = new ConcurrentDictionary<Type, int>();
        private int queueIndex;
        private int currentTypeId = 0;
        private bool isDequeing;

        internal ConcurrentRequestBuffer()
        {
            for (int i = 0; i < this.queuedRequestChunks.Length; ++i)
            {
                this.queuedRequestChunks[i] = new ConcurrentBag<QueuedRequestChunk>();
            }
        }

        internal int Count
        {
            get
            {
                int count = 0;
                foreach (ConcurrentBag<QueuedRequestChunk> chunkBag in this.queuedRequestChunks)
                {
                    count += chunkBag.Count;
                }

                return count;
            }
        }

        internal int GetTypeId(Type type)
        {
            int value;
            if (this.typeIdsByType.TryGetValue(type, out value))
            {
                return value;
            }

            this.typeIdsByType.TryAdd(type, ++this.currentTypeId);
            return this.typeIdsByType[type];
        }

        internal void QueueRequest(QueuedRequestChunk item)
        {
            Contract.Requires(item != null);
            this.queuedRequestChunks[0].Add(item);
        }

        internal void RequeueRequest(QueuedRequestChunk item)
        {
            Contract.Requires(item != null);
            Contract.Assert(this.isDequeing);
            if (this.isDequeing)
            {
                if (this.queueIndex == this.queuedRequestChunks.Length - 1)
                {
                    this.queuedRequestChunks[this.queueIndex].Add(item);
                }
                else
                {
                    this.queuedRequestChunks[this.queueIndex + 1].Add(item);
                }
            }
        }

        internal IEnumerable<QueuedRequestChunk> DequeueRequests()
        {
            Contract.Assert(!this.isDequeing);
            if (!this.isDequeing)
            {
                Contract.Requires(this.queueIndex >= 0);
                this.isDequeing = true;

                try
                {
                    for (int j = this.queuedRequestChunks.Length - 1; j >= 0; --j)
                    {
                        this.queueIndex = j;
                        QueuedRequestChunk chunk;
                        if (this.queuedRequestChunks[j].TryTake(out chunk))
                        {
                            yield return chunk;
                        }
                    }
                }
                finally
                {
                    this.isDequeing = false;
                }
            }

            yield return null;
        }

        internal void CancelRequests()
        {
            Contract.Ensures(this.queuedRequestChunks.Length == 0);
            int canceled;
            do
            {
                canceled = 0;

                // Try to cancel any pending TCS objects.
                for (int i = 0; i < this.queuedRequestChunks.Length; ++i)
                {
                    QueuedRequestChunk request;
                    while (this.queuedRequestChunks[i].TryTake(out request))
                    {
                        request.Dispose();
                        ++canceled;
                    }
                }
            }
            while (canceled > 0);
        }
    }
}
