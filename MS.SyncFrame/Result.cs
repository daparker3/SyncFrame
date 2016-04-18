//-----------------------------------------------------------------------
// <copyright file="Result.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using ProtoBuf;

    /// <summary>
    /// Represent the result of a request.
    /// </summary>
    public class Result
    {
        private static MethodInfo serializeMethod = typeof(Serializer).GetMethods().Where((mi) => mi.Name == "Serialize").First();
        private static MethodInfo deserializeMethod = typeof(Serializer).GetMethods().Where((mi) => mi.Name == "Deserialize").First();
        private MessageTransport localTransport;
        private int requestId;
        private bool remote;

        internal Result(MessageTransport localTransport, MessageHeader header, Stream s)
        {
            Contract.Requires(localTransport != null);
            Contract.Requires(header != null);
            Contract.Requires(s != null);
            this.localTransport = localTransport;
            this.remote = true;
            this.requestId = header.RequestId;
            if (header.Faulted)
            {
                MethodInfo faultDeserializer = deserializeMethod.MakeGenericMethod(header.DataType);
                object faultObject = faultDeserializer.Invoke(null, new object[] { s });
                Type faultObjectType = faultObject.GetType();
                Type faultExceptionType = typeof(FaultException<>).MakeGenericType(faultObjectType);
                ConstructorInfo faultExceptionConstructor = faultExceptionType.GetConstructor(new Type[] { faultObjectType });
                throw faultExceptionConstructor.Invoke(new object[] { faultObject }) as Exception;
            }
        }

        internal Result(MessageTransport localTransport, int requestId)
        {
            Contract.Requires(localTransport != null);
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

        internal void Write<T>(Stream s, T value, bool isFault, bool isResponse) where T : class
        {
            Contract.Requires(s != null);
            Contract.Requires(value != null);
            MessageHeader header = new MessageHeader
            {
                Faulted = isFault,
                Response = isResponse,
                RequestId = this.requestId
            };

            header.DataType = value.GetType();
            Serializer.SerializeWithLengthPrefix(s, header, PrefixStyle.Base128);
            if (value != null)
            {
                int start = (int)s.Position;
                Serializer.Serialize(s, value);
                s.Position = start;
            }
        }
    }
}
