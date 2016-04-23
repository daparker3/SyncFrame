//-----------------------------------------------------------------------
// <copyright file="FrameHeader.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System;
    using ProtoBuf;

    [Flags]
    internal enum FrameHeaderFlags
    {
        None = 0x0,
        InternalError = 0x1
    }

    [ProtoContract]
    internal class FrameHeader
    {
        [ProtoMember(1)]
        internal FrameHeaderFlags HeaderFlags { get; set; }

        [ProtoMember(2)]
        internal FrameType[] Types { get; set; }

        [ProtoMember(3, IsPacked = true)]
        internal int[] MessageSizes { get; set; }
    }
}
