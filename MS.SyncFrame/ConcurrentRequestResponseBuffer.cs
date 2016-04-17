//-----------------------------------------------------------------------
// <copyright file="ConcurrentRequestResponseBuffer.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Threading.Tasks;
    using Properties;

    internal class ConcurrentRequestResponseBuffer
    {
        private ConcurrentDictionary<int, WeakReference<QueuedRequestResponseChunk>> pendingResponsesByRequest = new ConcurrentDictionary<int, WeakReference<QueuedRequestResponseChunk>>();

        internal int Count
        {
            get
            {
                return this.pendingResponsesByRequest.Count;
            }
        }

        internal QueuedRequestResponseChunk CreateResponse(Stream dataStream, int requestId)
        {
            if (this.pendingResponsesByRequest.ContainsKey(requestId))
            {
                throw new InvalidOperationException(Resources.RequestAlreadyInProgress);
            }

            // This request originates from us; set up our response handler.
            QueuedRequestResponseChunk responseChunk = new QueuedRequestResponseChunk(dataStream);

            //// To prevent the response chunk going out of scope before the user can get it, we reference it in our
            //// return value. That way, if it actually does go out of scope we can catch it with a runtime error.
            WeakReference<QueuedRequestResponseChunk> responseWeakRef = new WeakReference<QueuedRequestResponseChunk>(responseChunk);
            if (!this.pendingResponsesByRequest.TryAdd(requestId, responseWeakRef))
            {
                throw new InvalidOperationException(Resources.TheResponseWasCompletedMultipleTimes);
            }

            responseChunk.PostCompleteTask = this.PostComplete(responseWeakRef, requestId);
            return responseChunk;
        }

        internal bool TryGetResponse(int requestId, out QueuedRequestResponseChunk qrc)
        {
            qrc = null;
            WeakReference<QueuedRequestResponseChunk> weakQrc;
            if (this.pendingResponsesByRequest.TryGetValue(requestId, out weakQrc))
            {
                if (weakQrc.TryGetTarget(out qrc))
                {
                    return true;
                }
            }

            return false;
        }

        internal void CancelResponses()
        {
            int canceled;
            do
            {
                canceled = 0;

                foreach (int requestId in this.pendingResponsesByRequest.Keys)
                {
                    WeakReference<QueuedRequestResponseChunk> responseWeakRef;
                    while (this.pendingResponsesByRequest.TryRemove(requestId, out responseWeakRef))
                    {
                        QueuedRequestResponseChunk response;
                        if (responseWeakRef.TryGetTarget(out response))
                        {
                            response.Dispose();
                            ++canceled;
                        }
                    }
                }
            }
            while (canceled > 0);
        }

        private Task PostComplete(WeakReference<QueuedRequestResponseChunk> responseWeakRef, int requestId)
        {
            return Task.Run(() =>
            {
                WeakReference<QueuedRequestResponseChunk> removedResponseWeakRef;
                if (!this.pendingResponsesByRequest.TryRemove(requestId, out removedResponseWeakRef))
                {
                    throw new InvalidOperationException(Resources.TheResponseWasCompletedMultipleTimes);
                }

                if (removedResponseWeakRef != responseWeakRef)
                {
                    throw new InvalidOperationException(Resources.RequestAlreadyInProgress);
                }
            });
        }
    }
}
