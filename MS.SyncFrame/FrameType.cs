//-----------------------------------------------------------------------
// <copyright file="FrameType.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System;
    using ProtoBuf;

    [ProtoContract]
    internal class FrameType
    {
        [ProtoMember(1)]
        internal int TypeId { get; set; }

        [ProtoMember(2)]
        internal Type Type { get; set; }
    }
}
