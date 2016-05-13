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
        private string objectNamespace;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProtoServiceContractAttribute"/> class.
        /// </summary>
        /// <param name="objectNamespace">The namespace path for generated objects.</param>
        public ProtoServiceContractAttribute(string objectNamespace)
        {
            Ensure.That(objectNamespace, "objectNamespace").IsNotNullOrEmpty();
            this.objectNamespace = objectNamespace;
            this.ObjectAssemblyName = "protocontract";
        }

        /// <summary>
        /// Gets or sets the name of the object assembly.
        /// </summary>
        /// <value>
        /// The name of the object assembly.
        /// </value>
        /// <remarks>Generated code for this service contract will be placed in an assembly with this name.</remarks>
        public string ObjectAssemblyName
        {
            get;
            set;
        }
    }
}
