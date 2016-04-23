//-----------------------------------------------------------------------
// <copyright file="MessageHeader.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System;
    using ProtoBuf;

    [Flags]
    internal enum HeaderFlags
    {
        None = 0x0,
        Faulted = 0x1,
        Response = 0x2
    }

    [ProtoContract]
    internal class MessageHeader
    {
        [ProtoMember(1, DataFormat = DataFormat.FixedSize)]
        internal int RequestId { get; set; }

        [ProtoMember(2)]
        internal int TypeId { get; set; }

        [ProtoMember(3)]
        internal HeaderFlags Flags { get; set; }
    }
}
