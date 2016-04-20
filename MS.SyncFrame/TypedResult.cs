//-----------------------------------------------------------------------
// <copyright file="TypedResult.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System.Diagnostics.Contracts;
    using System.IO;
    using EnsureThat;
    using ProtoBuf;

    /// <summary>
    /// Represent a typed result.
    /// </summary>
    /// <typeparam name="TResult">The type of the result to fetch.</typeparam>
    public class TypedResult<TResult> : Result where TResult : class
    {
        private TResult result;

        internal TypedResult(MessageTransport localTransport, MessageHeader header, Stream s)
            : base(localTransport, header, s)
        {
            Contract.Requires(localTransport != null);
            Contract.Requires(header != null);
            Contract.Requires(s != null);
            Contract.Ensures(this.result != null);
            this.result = Serializer.DeserializeWithLengthPrefix<TResult>(s, PrefixStyle.Base128);
        }

        internal TypedResult(MessageTransport localTransport, int requestId, TResult result)
            : base(localTransport, requestId)
        {
            Contract.Requires(localTransport != null);
            Contract.Requires(result != null);
            this.result = result;
        }

        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <value>
        /// The data.
        /// </value>
        public TResult Data
        {
            get
            {
                return this.result;
            }
        }
    }
}
