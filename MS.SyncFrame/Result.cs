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
    using EnsureThat;
    using ProtoBuf;

    /// <summary>
    /// Represent the result of a request.
    /// </summary>
    public class Result
    {
        private static MethodInfo deserializeMethod = typeof(Serializer).GetMethods().Where((mi) => mi.Name == "DeserializeWithLengthPrefix").First();
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
                object faultObject = faultDeserializer.Invoke(null, new object[] { s, PrefixStyle.Base128 });
                Type faultExceptionType = typeof(FaultException<>).MakeGenericType(header.DataType);
                ConstructorInfo faultExceptionConstructor = faultExceptionType.GetConstructor(new Type[] { typeof(Result), header.DataType });
                Exception faultEx = faultExceptionConstructor.Invoke(new object[] { this, faultObject }) as Exception;
                this.localTransport.SetFault(faultEx);
                throw faultEx;
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

        /// <summary>
        /// Sends the data as a response to a remote request.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="data">The data.</param>
        /// <returns>A <see cref="Task{Result}"/> which completes with the message when sent.</returns>
        /// <remarks>See the <see cref="MessageTransport.SendData{TRequest}(TRequest)"/> method for a list of exceptions that can be thrown.</remarks>
        public async Task<Result> SendData<TResponse>(TResponse data) where TResponse : class
        {
            Ensure.That(data, "data").IsNotNull();
            Contract.Requires(this.LocalTransport != null);
            Contract.Requires(this.Remote);
            return await this.LocalTransport.SendData(this, data);
        }

        /// <summary>
        /// Sends a fault as a response to a remote request.
        /// </summary>
        /// <typeparam name="TFault">The type of the fault.</typeparam>
        /// <param name="fault">The fault.</param>
        /// <returns>A <see cref="Task{FaultException}"/> which contains information about the fault and can be thrown to terminate the session.</returns>
        public async Task<FaultException<TFault>> SendFault<TFault>(TFault fault) where TFault : class
        {
            Ensure.That(fault, "fault").IsNotNull();
            Contract.Requires(this.LocalTransport != null);
            Contract.Requires(this.Remote);
            return await this.LocalTransport.SendFault(this, fault);
        }

        internal void Write<T>(Stream s, T value, bool isFault, bool isResponse) where T : class
        {
            Contract.Requires(s != null);
            Contract.Requires(value != null);
            MessageHeader header = new MessageHeader
            {
                Faulted = isFault,
                Response = isResponse,
                RequestId = this.requestId,
                DataType = value.GetType()
            };
            
            Serializer.SerializeWithLengthPrefix(s, header, PrefixStyle.Base128);
            long startData = s.Position;
            Serializer.SerializeWithLengthPrefix(s, value, PrefixStyle.Base128);
            s.Position = 0;
        }
    }
}
