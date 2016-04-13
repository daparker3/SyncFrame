//-----------------------------------------------------------------------
// <copyright file="RequestResult.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System.IO;

    /// <summary>
    /// Represent the result of a request.
    /// </summary>
    /// <seealso cref="MS.SyncFrame.Result" />
    public class RequestResult : Result
    {
        internal RequestResult(MessageTransport localTransport, MessageHeader header, Stream s)
            : base(localTransport, header, s)
        {
        }

        internal RequestResult(MessageTransport localTransport, long requestId)
            : base(localTransport, requestId)
        {
        }
        
        internal QueuedResponseChunk ResponseChunk
        {
            get;
            set;
        }
    }
}
