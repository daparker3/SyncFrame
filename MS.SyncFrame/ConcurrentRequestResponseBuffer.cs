//-----------------------------------------------------------------------
// <copyright file="ConcurrentRequestResponseBuffer.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System;
    using System.Collections.Concurrent;
    using System.Globalization;
    using Properties;

    internal class ConcurrentRequestResponseBuffer
    {
        private ConcurrentDictionary<long, WeakReference<QueuedResponseChunk>> pendingResponsesByRequest = new ConcurrentDictionary<long, WeakReference<QueuedResponseChunk>>();

        internal int Count
        {
            get
            {
                return this.pendingResponsesByRequest.Count;
            }
        }

        internal QueuedResponseChunk CreateResponse(long requestId)
        {
            if (this.pendingResponsesByRequest.ContainsKey(requestId))
            {
                throw new InvalidOperationException(Resources.RequestAlreadyInProgress);
            }

            // This request originates from us; set up our response handler.
            QueuedResponseChunk responseChunk = new QueuedResponseChunk();

            //// To prevent the response chunk going out of scope before the user can get it, we reference it in our
            //// return value. That way, if it actually does go out of scope we can catch it with a runtime error.
            WeakReference<QueuedResponseChunk> responseWeakRef = new WeakReference<QueuedResponseChunk>(responseChunk);
            if (!this.pendingResponsesByRequest.TryAdd(requestId, responseWeakRef))
            {
                throw new InvalidOperationException(Resources.TheResponseWasCompletedMultipleTimes);
            }

            responseChunk.ResponseCompletedTask = responseChunk.RequestCompleteTask.Task.ContinueWith((t) =>
            {
                WeakReference<QueuedResponseChunk> removedResponseWeakRef;
                if (!this.pendingResponsesByRequest.TryRemove(requestId, out removedResponseWeakRef))
                {
                    throw new InvalidOperationException(Resources.TheResponseWasCompletedMultipleTimes);
                }

                if (removedResponseWeakRef != responseWeakRef)
                {
                    throw new InvalidOperationException(Resources.RequestAlreadyInProgress);
                }
            });

            return responseChunk;
        }

        internal bool TryGetResponse(long requestId, out QueuedResponseChunk qrc)
        {
            qrc = null;
            WeakReference<QueuedResponseChunk> weakQrc;
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

                foreach (long requestId in this.pendingResponsesByRequest.Keys)
                {
                    WeakReference<QueuedResponseChunk> responseWeakRef;
                    while (this.pendingResponsesByRequest.TryRemove(requestId, out responseWeakRef))
                    {
                        QueuedResponseChunk response;
                        if (responseWeakRef.TryGetTarget(out response))
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
    }
}
