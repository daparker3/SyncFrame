//-----------------------------------------------------------------------
// <copyright file="QueuedResponseChunk.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System.IO;
    using System.Threading.Tasks;

    internal class QueuedResponseChunk : QueuedChunk
    {
        internal QueuedResponseChunk(Stream dataStream)
            : base(dataStream)
        {
        }

        internal MessageHeader Header { get; set; }

        internal virtual async Task ResponseComplete()
        {
            await this.CompleteTask.Task;
        }
    }
}