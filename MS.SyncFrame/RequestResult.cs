//-----------------------------------------------------------------------
// <copyright file="RequestResult.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System;
    using System.Diagnostics.Contracts;
    using System.IO;

    /// <summary>
    /// Represent the result of a request.
    /// </summary>
    /// <seealso cref="MS.SyncFrame.Result" />
    public class RequestResult : Result
    {
        private QueuedResponseChunk responseChunk;

        internal RequestResult(MessageTransport localTransport, Type dataType, MessageHeader header, Stream s)
            : base(localTransport, dataType, header, s)
        {
            Contract.Requires(localTransport != null);
            Contract.Requires(dataType != null);
            Contract.Requires(header != null);
            Contract.Requires(s != null);
        }

        internal RequestResult(MessageTransport localTransport, int requestId)
            : base(localTransport, requestId)
        {
            Contract.Requires(localTransport != null);
        }
        
        internal QueuedResponseChunk ResponseChunk
        {
            get
            {
                return this.responseChunk;
            }

            set
            {
                Contract.Requires(value != null);
                this.responseChunk = value;
            }
        }
    }
}
