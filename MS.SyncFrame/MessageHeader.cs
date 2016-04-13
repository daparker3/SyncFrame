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
        [ProtoMember(1)]
        internal Type DataType { get; set; }

        [ProtoMember(2, DataFormat = DataFormat.ZigZag)]
        internal long RequestId { get; set; }

        [ProtoMember(3)]
        internal long DataSize { get; set; }

        [ProtoMember(4)]
        internal bool Faulted { get; set; }

        [ProtoMember(5)]
        internal bool Response { get; set; }
    }
}
