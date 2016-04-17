//-----------------------------------------------------------------------
// <copyright file="QueuedRequestChunk.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System.IO;
    using System.Threading.Tasks;

    internal class QueuedRequestChunk : QueuedChunk
    {
        internal QueuedRequestChunk(Stream dataStream)
            : base(dataStream)
        {
        }

        internal virtual async Task ResponseComplete()
        {
            await this.CompleteTask.Task;
        }
    }
}
