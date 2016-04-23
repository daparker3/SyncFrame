//-----------------------------------------------------------------------
// <copyright file="MessageHeader.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System;
    using ProtoBuf;

    [ProtoContract]
    internal class MessageHeader
    {
        [ProtoMember(1, DataFormat = DataFormat.ZigZag)]
        internal int RequestId { get; set; }

        [ProtoMember(2)]
        internal int DataTypeIndex { get; set; }

        [ProtoMember(3)]
        internal bool Faulted { get; set; }

        [ProtoMember(4)]
        internal bool Response { get; set; }
    }
}
