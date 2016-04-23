//-----------------------------------------------------------------------
// <copyright file="QueuedRequestChunk.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Threading.Tasks;

    internal class QueuedRequestChunk : QueuedChunk
    {
        internal QueuedRequestChunk(Stream dataStream, int typeId, Type type)
            : base(dataStream)
        {
            Contract.Requires(dataStream != null);
            Contract.Requires(type != null);
            this.TypeId = typeId;
            this.Type = type;
        }

        internal int TypeId { get; set; }

        internal Type Type { get; set; }

        internal virtual async Task ResponseComplete()
        {
            await this.CompleteTask.Task;
        }
    }
}
