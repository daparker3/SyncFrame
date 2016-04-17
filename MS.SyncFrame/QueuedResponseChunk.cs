//-----------------------------------------------------------------------
// <copyright file="QueuedResponseChunk.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Threading.Tasks;

    internal class QueuedResponseChunk : QueuedChunk
    {
        private MessageHeader header;

        internal QueuedResponseChunk(Stream dataStream)
            : base(dataStream)
        {
            Contract.Requires(dataStream != null);
        }

        internal MessageHeader Header
        {
            get
            {
                return this.header;
            }

            set
            {
                Contract.Requires(value != null);
                this.header = value;
            }
        }

        internal virtual async Task ResponseComplete()
        {
            await this.CompleteTask.Task;
        }
    }
}