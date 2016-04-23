﻿//-----------------------------------------------------------------------
// <copyright file="FrameHeader.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System;
    using ProtoBuf;

    [ProtoContract]
    internal class FrameHeader
    {
        [ProtoMember(2)]
        internal FrameType[] Types { get; set; }

        [ProtoMember(1)]
        internal int[] MessageSizes { get; set; }
    }
}
