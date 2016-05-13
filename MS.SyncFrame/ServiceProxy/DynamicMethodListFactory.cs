//-----------------------------------------------------------------------
// <copyright file="DynamicMethodListFactory.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace MS.SyncFrame.ServiceProxy
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Reflection;
    using ProtoBuf;

    internal class DynamicMethodListFactory<T> where T : class
    {
        private static readonly Type interfaceType = typeof(T);
        private ProtoServiceContractAttribute serviceContractAttribute = interfaceType.GetCustomAttributes(typeof(ProtoServiceContractAttribute), true).First() as ProtoServiceContractAttribute;
        private Dictionary<int, DynamicMethodInfo> methodInfoByMethodId = new Dictionary<int, DynamicMethodInfo>();

        internal DynamicMethodListFactory()
        {
            Contract.Requires(interfaceType.IsInterface);
            foreach (MethodInfo mi in interfaceType.GetMethods())
            {
                object[] attributes = mi.GetCustomAttributes(typeof(ProtoServiceMemberAttribute), true);
                if (!mi.IsGenericMethod && attributes != null && attributes.Length > 0)
                {
                    ProtoServiceMemberAttribute ma = attributes[0] as ProtoServiceMemberAttribute;
                    Contract.Assert(!this.methodInfoByMethodId.ContainsKey(ma.MethodId));
                    if (!this.methodInfoByMethodId.ContainsKey(ma.MethodId))
                    {
                        // We'll pass all of the below through to the DMI object.
                        ProtoContractAttribute contractAttribute = mi.GetCustomAttribute(typeof(ProtoContractAttribute)) as ProtoContractAttribute;
                        ParameterInfo[] parameters = mi.GetParameters();
                        DynamicMethodInfo dmi = new DynamicMethodInfo(this.serviceContractAttribute, contractAttribute, ma, mi.ReturnParameter, parameters);
                        this.methodInfoByMethodId[ma.MethodId] = dmi;
                    }
                }
            }
        }

        internal object CreateRequest(int methodId, object[] requestParams)
        {
            Contract.Requires(this.methodInfoByMethodId.ContainsKey(methodId));
            DynamicMethodInfo dmi = this.methodInfoByMethodId[methodId];
            return dmi.CreateRequestMessage(requestParams);
        }

        internal Type GetRequestType(int methodId)
        {
            Contract.Requires(this.methodInfoByMethodId.ContainsKey(methodId));
            DynamicMethodInfo dmi = this.methodInfoByMethodId[methodId];
            return dmi.GetRequestType();
        }

        internal object CreateResponse(int methodId, object responseParam)
        {
            Contract.Requires(this.methodInfoByMethodId.ContainsKey(methodId));
            DynamicMethodInfo dmi = this.methodInfoByMethodId[methodId];
            return dmi.CreateResponseMessage(responseParam);
        }
        
        internal Type GetResponseType(int methodId)
        {
            Contract.Requires(this.methodInfoByMethodId.ContainsKey(methodId));
            DynamicMethodInfo dmi = this.methodInfoByMethodId[methodId];
            return dmi.GetResponseType();
        }

        private class DynamicMethodInfo
        {
            private Type requestType;
            private Type responseType;
            private Func<object[], object> createRequestObjectAction;
            private Func<object, object> createResponseObjectAction;

            internal DynamicMethodInfo(
                ProtoServiceContractAttribute serviceContractAttribute, 
                ProtoContractAttribute contractAttribute, 
                ProtoServiceMemberAttribute memberAttribute, 
                ParameterInfo returnParameter, 
                ParameterInfo[] parameters
            )
            {
                Contract.Requires(serviceContractAttribute != null);
                Contract.Ensures(this.createRequestObjectAction != null);
                Contract.Ensures(this.createResponseObjectAction != null);

                // Prepare the request/response action factory.
                this.createRequestObjectAction = (p) =>
                {
                    throw new NotImplementedException();
                };

                if (!memberAttribute.IsOneWay)
                {
                    this.createResponseObjectAction = (p) =>
                    {
                        throw new NotImplementedException();
                    };
                }
            }

            internal Type GetRequestType()
            {
                return this.requestType;
            }

            internal Type GetResponseType()
            {
                return this.responseType;
            }

            internal object CreateRequestMessage(object[] requestParams)
            {
                return this.createRequestObjectAction(requestParams);
            }

            internal object CreateResponseMessage(object responseParam)
            {
                if (this.createResponseObjectAction != null)
                {
                    return this.createResponseObjectAction(responseParam);
                }

                return null;
            }
        }
    }
}
