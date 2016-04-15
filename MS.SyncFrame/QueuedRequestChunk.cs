//-----------------------------------------------------------------------
// <copyright file="QueuedRequestChunk.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System.Threading.Tasks;

    internal class QueuedRequestChunk : QueuedChunk
    {
        internal virtual async Task ResponseComplete()
        {
            await this.CompleteTask.Task;
        }
    }
}
