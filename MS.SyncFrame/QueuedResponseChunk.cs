//-----------------------------------------------------------------------
// <copyright file="QueuedResponseChunk.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    internal class QueuedResponseChunk : QueuedChunk
    {
        internal QueuedResponseChunk()
        {
        }

        internal MessageHeader Header { get; set; }
    }
}