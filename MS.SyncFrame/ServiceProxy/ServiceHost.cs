//-----------------------------------------------------------------------
// <copyright file="ServiceHost.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame.ServiceProxy
{
    using System;
    using System.Threading.Tasks;
    using ProtoBuf;

    /// <summary>
    /// Represent a service host for the given contract over an interface.
    /// </summary>
    /// <typeparam name="TContract">The type of contract interface to proxy.</typeparam>
    /// <remarks>
    /// <para>You must derive from this class and the interface represented by <typeparamref name="TContract"/> or you'll receive a runtime error when instantiated objects of this type.</para>
    /// <para>The <typeparamref name="TContract"/> must have the following properties:</para>
    /// <list type="bullet">
    /// <item><term>The interface must be decorated with an <see cref="ProtoServiceContractAttribute"/> attribute.</term></item>
    /// <item><term>Each member of the interface you want to proxy must be decorated with an <see cref="ProtoServiceMemberAttribute"/> attribute.</term></item>
    /// <item><term>Each member of the interface you want to proxy must be a method with a return type of <see cref="Task"/> or <see cref="Task{TResult}"/>.</term></item>
    /// <item><term>Each method must contain 0 or 1 parameters.</term></item>
    /// <item><term>If a method contains a parameter, the type of that parameter will be serialized in the same way that a message parameter to <see cref="MessageTransport.SendData{TRequest}(TRequest)"/> would be. If the parameter is a value type, a boxing conversion to a reference type may be applied.</term></item>
    /// <item><term>If a method returns a <see cref="Task{TResult}"/>, in the same way that a message parameter to <see cref="MessageTransport.SendData{TRequest}(TRequest)"/> would be. If the parameter is a value type, a boxing conversion to a reference type may be applied.</term></item>
    /// </list>
    /// <para>If each of these conditions are not met, a runtime error will be generated.</para>
    /// </remarks>
    public abstract class ServiceHost<TContract> : IDisposable where TContract : class
    {
        private bool disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceHost{TContract}"/> class.
        /// </summary>
        /// <param name="server">The message server.</param>
        /// <remarks>After you call the constructor, the service host will use the <see cref="MessageServer"/> object to remote messages.</remarks>
        public ServiceHost(MessageServer server)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="ServiceHost{TContract}"/> class.
        /// </summary>
        ~ServiceHost()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                this.disposed = true;

                if (disposing)
                {
                }
            }
        }
    }
}
