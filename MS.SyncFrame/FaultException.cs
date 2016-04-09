//-----------------------------------------------------------------------
// <copyright file="FaultException.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame
{
    using System;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using EnsureThat;
    using Properties;

    /// <summary>
    /// Represent an exception which can marshal a SyncFrame fault across remote boundaries.
    /// </summary>
    /// <typeparam name="TFault">The type of the fault.</typeparam>
    /// <seealso cref="System.Exception" />
    [Serializable]
    public class FaultException<TFault> : Exception where TFault : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FaultException{TFault}"/> class.
        /// </summary>
        public FaultException() : base(Resources.ASyncFaultOccured)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FaultException{TFault}"/> class.
        /// </summary>
        /// <param name="faultingRequest">The request.</param>
        /// <param name="fault">The fault.</param>
        /// <remarks>
        /// If you don't specify the original request which generated the fault, the fault may not be reported to the other transport and your session may continue running as if nothing happened.
        /// </remarks>
        public FaultException(Result faultingRequest, TFault fault) 
            : base(Resources.ASyncFaultOccured) 
        {
            Ensure.That(faultingRequest, "faultingRequest").IsNotNull();
            Ensure.That(fault, "fault").IsNotNull();
            this.Request = faultingRequest;
            this.Fault = fault;
            if (this.Request.LocalTransport != null)
            {
                if (this.Request.Remote)
                {
                    // We're responding to a remote request, but our request generated a fault. Send the fault back to the remote.
                    this.Request.LocalTransport.SendData(this.Request, fault, true);
                }

                this.Request.LocalTransport.SetFault(this);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FaultException{TFault}"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
        protected FaultException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            this.Fault = (TFault)info.GetValue("fault", typeof(TFault));
            this.Request = (Result)info.GetValue("result", typeof(Result));
        }

        /// <summary>
        /// Gets the request.
        /// </summary>
        /// <value>
        /// The request.
        /// </value>
        public Result Request
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the fault.
        /// </summary>
        /// <value>
        /// The fault.
        /// </value>
        public TFault Fault
        {
            get;
            private set;
        }

        /// <summary>
        /// When overridden in a derived class, sets the <see cref="T:System.Runtime.Serialization.SerializationInfo" /> with information about the exception.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
        /// <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Read="*AllFiles*" PathDiscovery="*AllFiles*" />
        ///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="SerializationFormatter" />
        /// </PermissionSet>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("fault", this.Fault);
            info.AddValue("result", this.Request);
        }
    }
}
