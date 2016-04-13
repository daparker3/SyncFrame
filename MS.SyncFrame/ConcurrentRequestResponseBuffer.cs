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
            if (!this.pendingResponsesByRequest.TryAdd(requestId, new WeakReference<QueuedResponseChunk>(responseChunk)))
            {
                throw new InvalidOperationException(Resources.TheResponseWasCompletedMultipleTimes);
            }

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

        internal void CheckLeakedRequests()
        {
            // Check for user errors by seeing if any request that has not been responded to has leaked.
            foreach (long requestId in this.pendingResponsesByRequest.Keys)
            {
                WeakReference<QueuedResponseChunk> weakQrc;
                if (this.pendingResponsesByRequest.TryGetValue(requestId, out weakQrc))
                {
                    QueuedResponseChunk qrc;
                    if (!weakQrc.TryGetTarget(out qrc))
                    {
                        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.ARequestOfTypeLeaked, Resources.Unknown));
                    }
                }
            }
        }

        internal bool TryCompleteResponse(long requestId)
        {
            WeakReference<QueuedResponseChunk> qrc;
            return this.pendingResponsesByRequest.TryRemove(requestId, out qrc);
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
