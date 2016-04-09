//-----------------------------------------------------------------------
// <copyright file="Result.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System;
    using System.IO;
    using System.Reflection;
    using EnsureThat;
    using ProtoBuf;

    /// <summary>
    /// Represent the result of a request.
    /// </summary>
    public class Result
    {
        private static MethodInfo serializeMethod = typeof(Serializer).GetMethod("SerializeWithLengthPrefix");
        private static MethodInfo deserializeMethod = typeof(Serializer).GetMethod("DeserializeWithLengthPrefix");
        private MessageTransport localTransport;
        private int requestId;
        private bool remote;

        internal Result(MessageTransport localTransport, Stream s)
        {
            Ensure.That(s, "s").IsNotNull();
            this.localTransport = localTransport;
            this.remote = true;
            MessageHeader header = Serializer.DeserializeWithLengthPrefix<MessageHeader>(s, PrefixStyle.Base128);
            this.requestId = header.RequestId;
            if (header.Faulted)
            {
                MethodInfo faultDeserializer = deserializeMethod.MakeGenericMethod(header.DataType);
                object faultObject = faultDeserializer.Invoke(null, new object[] { s, PrefixStyle.Base128 });
                Type faultObjectType = faultObject.GetType();
                Type faultExceptionType = typeof(FaultException<>).MakeGenericType(faultObjectType);
                ConstructorInfo faultExceptionConstructor = faultExceptionType.GetConstructor(new Type[] { faultObjectType });
                throw faultExceptionConstructor.Invoke(new object[] { faultObject }) as Exception;
            }
        }

        internal Result(MessageTransport localTransport, int requestId)
        {
            this.localTransport = localTransport;
            this.remote = false;
            this.requestId = requestId;
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Result"/> is remote.
        /// </summary>
        /// <value>
        ///   <c>true</c> if remote; otherwise, <c>false</c>.
        /// </value>
        public bool Remote
        {
            get
            {
                return this.remote;
            }
        }

        /// <summary>
        /// Gets the request identifier.
        /// </summary>
        /// <value>
        /// The request identifier.
        /// </value>
        public int RequestId
        {
            get
            {
                return this.requestId;
            }
        }

        internal MessageTransport LocalTransport
        {
            get
            {
                return this.localTransport;
            }
        }

        internal void Write<T>(Stream s, T value, bool isFault) where T : class
        {
            Ensure.That(s, "s").IsNotNull();
            MessageHeader header = new MessageHeader
            {
                Faulted = isFault,
                RequestId = this.requestId
            };

            if (value == null)
            {
                Ensure.That(isFault, "isFault").IsFalse();
            }
            else
            {
                header.DataType = value.GetType();
            }

            Serializer.SerializeWithLengthPrefix(s, header, PrefixStyle.Base128);
            if (value != null)
            {
                Serializer.SerializeWithLengthPrefix<T>(s, value, PrefixStyle.Base128);
            }
        }
    }
}
