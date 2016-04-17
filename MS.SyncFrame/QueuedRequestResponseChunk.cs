//-----------------------------------------------------------------------
// <copyright file="QueuedRequestResponseChunk.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Threading.Tasks;

    internal class QueuedRequestResponseChunk : QueuedResponseChunk
    {
        internal QueuedRequestResponseChunk(Stream dataStream)
            : base(dataStream)
        {
            Contract.Requires(dataStream != null);
        }

        internal Task PostCompleteTask
        {
            get;
            set;
        }

        internal override async Task ResponseComplete()
        {
            await base.ResponseComplete();
            if (this.PostCompleteTask != null)
            {
                await this.PostCompleteTask;
            }
        }
    }
}