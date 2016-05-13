//-----------------------------------------------------------------------
// <copyright file="ServiceClient.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame.ServiceProxy
{
    using System;

    /// <summary>
    /// Represent a service client factory for the specified interface. For more information about defining the interface, see the documentation for <see cref="ServiceHost{TContract}"/>.
    /// </summary>
    /// <typeparam name="TContract">The type of contract interface to proxy.</typeparam>
    public class ServiceClient<TContract> : IDisposable where TContract : class
    {
        private bool disposed = false;

        private ServiceClient()
        {
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="ServiceClient{TContract}"/> class.
        /// </summary>
        ~ServiceClient()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Creates the service proxy.
        /// </summary>
        /// <param name="client">The message client.</param>
        /// <returns>A proxy of type TContract, which is valid until the client is closed.</returns>
        /// <remarks>To properly free managed resources, the return value of this method is always convertible to an <see cref="IDisposable"/> interface.</remarks>
        public static TContract CreateProxy(MessageClient client)
        {
            throw new NotImplementedException();
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
