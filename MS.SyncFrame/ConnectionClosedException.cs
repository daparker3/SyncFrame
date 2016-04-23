//-----------------------------------------------------------------------
// <copyright file="ConnectionClosedException.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System;

    /// <summary>
    /// Occurs when the connection is closed by the sender.
    /// </summary>
    /// <seealso cref="System.Exception" />
    [Serializable]
    public class ConnectionClosedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionClosedException"/> class.
        /// </summary>
        public ConnectionClosedException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionClosedException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ConnectionClosedException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionClosedException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner.</param>
        public ConnectionClosedException(string message, Exception inner) : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionClosedException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
        protected ConnectionClosedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }
    }
}
