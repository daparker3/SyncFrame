//-----------------------------------------------------------------------
// <copyright file="QueuedRequestChunk.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Threading.Tasks;

    internal class QueuedRequestChunk : QueuedChunk
    {
        internal QueuedRequestChunk(Stream dataStream)
            : base(dataStream)
        {
            Contract.Requires(dataStream != null);
        }

        internal virtual async Task ResponseComplete()
        {
            await this.CompleteTask.Task;
        }
    }
}
