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
        [ProtoMember(1, IsRequired = true)]
        internal long RequestId { get; set; }

        [ProtoMember(2, IsRequired = true)]
        internal bool Faulted { get; set; }

        [ProtoMember(3, IsRequired = true)]
        internal Type DataType { get; set; }
    }
}
