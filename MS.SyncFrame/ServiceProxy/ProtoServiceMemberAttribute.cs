//-----------------------------------------------------------------------
// <copyright file="ProtoServiceMemberAttribute.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame.ServiceProxy
{
    using EnsureThat;

    /// <summary>
    /// Represent protocol buffer service member attribute.
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [System.AttributeUsage(System.AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class ProtoServiceMemberAttribute : System.Attribute
    {
        private int methodId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProtoServiceMemberAttribute"/> class.
        /// </summary>
        /// <param name="methodId">The method identifier, which must be unique across all methods in the interface.</param>
        public ProtoServiceMemberAttribute(int methodId)
        {
            Ensure.That(methodId, "methodId").IsGte(0);
            this.methodId = methodId;
        }

        /// <summary>
        /// Gets the method identifier.
        /// </summary>
        /// <value>
        /// The method identifier.
        /// </value>
        public int MethodId
        {
            get { return this.methodId; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is one way.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is one way; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>If the method is one way, we won't wait for a response from the client proxy.</remarks>
        public bool IsOneWay
        {
            get; set;
        }
    }
}
