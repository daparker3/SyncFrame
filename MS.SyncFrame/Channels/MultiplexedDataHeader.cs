//-----------------------------------------------------------------------
// <copyright file="MultiplexedDataHeader.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame.Channels
{
    using ProtoBuf;

    [ProtoContract]
    internal class MultiplexedDataHeader
    {
        [ProtoMember(1)]
        internal int ChannelId { get; set; }

        [ProtoMember(2)]
        internal int Length { get; set; }
    }
}
