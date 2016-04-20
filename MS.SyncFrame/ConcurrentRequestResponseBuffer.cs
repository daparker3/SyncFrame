//-----------------------------------------------------------------------
// <copyright file="ConcurrentRequestResponseBuffer.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics.Contracts;
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
            Contract.Requires(dataStream != null);
            bool hasRequest = this.pendingResponsesByRequest.ContainsKey(requestId);
            Contract.Assert(!hasRequest, Resources.TooManyRequests);

            // This request originates from us; set up our response handler.
            QueuedRequestResponseChunk responseChunk = new QueuedRequestResponseChunk(dataStream);

            //// To prevent the response chunk going out of scope before the user can get it, we reference it in our
            //// return value. That way, if it actually does go out of scope we can catch it with a runtime error.
            WeakReference<QueuedRequestResponseChunk> responseWeakRef = new WeakReference<QueuedRequestResponseChunk>(responseChunk);
            hasRequest = this.pendingResponsesByRequest.TryAdd(requestId, responseWeakRef);
            Contract.Assert(hasRequest, Resources.TheResponseWasCompletedMultipleTimes);
            responseChunk.PostCompleteTask = responseChunk.CompleteTask.Task.ContinueWith((t) => this.PostComplete(responseWeakRef, requestId));
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
            Contract.Ensures(this.pendingResponsesByRequest.Count == 0);
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

        private void PostComplete(WeakReference<QueuedRequestResponseChunk> responseWeakRef, int requestId)
        {
            Contract.Requires(responseWeakRef != null);
            WeakReference<QueuedRequestResponseChunk> removedResponseWeakRef;
            bool removedRequest = this.pendingResponsesByRequest.TryRemove(requestId, out removedResponseWeakRef);
            Contract.Assert(removedRequest, Resources.TheResponseWasCompletedMultipleTimes);
            Contract.Assert(responseWeakRef == removedResponseWeakRef, Resources.RequestAlreadyInProgress);
        }
    }
}
