//-----------------------------------------------------------------------
// <copyright file="MissingMessageHandler.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System;
    using ProtoBuf;

    /// <summary>
    /// Represent a fault which can occur if a message transport is missing a required message handler.
    /// </summary>
    [ProtoContract]
    public class MissingMessageHandler
    {
        /// <summary>
        /// Gets or sets the type of the message.
        /// </summary>
        /// <value>
        /// The type of the message.
        /// </value>
        [ProtoMember(1)]
        public Type MessageType { get; set; }
    }
}
