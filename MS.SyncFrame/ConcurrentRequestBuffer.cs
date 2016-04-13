﻿//-----------------------------------------------------------------------
// <copyright file="ConcurrentRequestBuffer.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    internal class ConcurrentRequestBuffer
    {
        private const int SizeRequestChunkRetryBuckets = 10;
        private ConcurrentBag<QueuedRequestChunk>[] queuedRequestChunks = new ConcurrentBag<QueuedRequestChunk>[SizeRequestChunkRetryBuckets];
        private int queueIndex;
        private bool isDequeing;

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

        internal void QueueRequest(QueuedRequestChunk item)
        {
            this.queuedRequestChunks[0].Add(item);
        }

        internal void RequeueRequest(QueuedRequestChunk item)
        {
            if (!this.isDequeing)
            {
                throw new InvalidOperationException();
            }

            if (this.queueIndex == this.queuedRequestChunks.Length - 1)
            {
                this.queuedRequestChunks[this.queueIndex].Add(item);
            }
            else
            {
                this.queuedRequestChunks[this.queueIndex + 1].Add(item);
            }
        }

        internal IEnumerable<QueuedRequestChunk> DequeueRequests()
        {
            if (this.isDequeing)
            {
                throw new InvalidOperationException();
            }

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

        internal void CancelRequests()
        {
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
                        request.RequestCompleteTask.TrySetCanceled();
                        request.Dispose();
                        ++canceled;
                    }
                }
            }
            while (canceled > 0);
        }
    }
}