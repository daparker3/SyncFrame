//-----------------------------------------------------------------------
// <copyright file="ProtoServiceContractAttribute.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame.ServiceProxy
{
    using EnsureThat;

    /// <summary>
    /// Represent a protocol buffer service contract attribute.
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [System.AttributeUsage(System.AttributeTargets.Interface, Inherited = true, AllowMultiple = false)]
    public sealed class ProtoServiceContractAttribute : System.Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProtoServiceContractAttribute"/> class.
        /// </summary>
        public ProtoServiceContractAttribute()
        {
        }
    }
}
