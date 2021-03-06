﻿//-----------------------------------------------------------------------
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
        private bool canceling = false;

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
            if (!hasRequest)
            {
                // This request originates from us; set up our response handler.
                QueuedRequestResponseChunk responseChunk = new QueuedRequestResponseChunk(dataStream);

                //// To prevent the response chunk going out of scope before the user can get it, we reference it in our
                //// return value. That way, if it actually does go out of scope we can catch it with a runtime error.
                WeakReference<QueuedRequestResponseChunk> responseWeakRef = new WeakReference<QueuedRequestResponseChunk>(responseChunk);
                hasRequest = this.pendingResponsesByRequest.TryAdd(requestId, responseWeakRef);
                Contract.Assert(hasRequest, Resources.TheResponseWasCompletedMultipleTimes);
                responseChunk.PostCompleteTask = this.PostResponseComplete(responseChunk.CompleteTask.Task, responseWeakRef, requestId);
                return responseChunk;
            }

            return null;
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
            this.canceling = true;
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

        private async Task PostResponseComplete(Task<bool> completeTask, WeakReference<QueuedRequestResponseChunk> responseWeakRef, int requestId)
        {
            await completeTask;
            this.PostComplete(responseWeakRef, requestId);
        }

        private void PostComplete(WeakReference<QueuedRequestResponseChunk> responseWeakRef, int requestId)
        {
            Contract.Requires(responseWeakRef != null);
            WeakReference<QueuedRequestResponseChunk> removedResponseWeakRef;
            bool removedRequest = this.pendingResponsesByRequest.TryRemove(requestId, out removedResponseWeakRef);
            if (!this.canceling)
            {
                Contract.Assert(removedRequest, Resources.TheResponseWasCompletedMultipleTimes);
                Contract.Assert(responseWeakRef == removedResponseWeakRef, Resources.RequestAlreadyInProgress);
            }
        }
    }
}
