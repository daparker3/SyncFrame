// <copyright file="FaultExceptionTFaultTest.cs" company="MS">Copyright © MS 2016</copyright>
using System;
using System.Runtime.Serialization;
using MS.SyncFrame;
using Microsoft.Pex.Framework;
using Microsoft.Pex.Framework.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MS.SyncFrame.Tests
{
    /// <summary>This class contains parameterized unit tests for FaultException`1</summary>
    [PexClass(typeof(FaultException<>))]
    [PexAllowedExceptionFromTypeUnderTest(typeof(ArgumentException), AcceptExceptionSubtypes = true)]
    [TestClass]
    public partial class FaultExceptionTFaultTest
    {
        /// <summary>Test stub for .ctor()</summary>
        [PexGenericArguments(typeof(int))]
        [PexMethod]
        public FaultException<TFault> FaultExceptionTFaultTest_ConstructorTest<TFault>() where TFault : class
        {
            FaultException<TFault> target = new FaultException<TFault>();
            Assert.IsNull(target.Request);
            Assert.IsNotNull(target.Fault);
            Assert.IsNotNull(target.Message);
            return target;
        }

        /// <summary>Test stub for .ctor(!0)</summary>
        [PexGenericArguments(typeof(int))]
        [PexMethod]
        public FaultException<TFault> FaultExceptionTFaultTest_ConstructorTest01<TFault>(Result request, TFault fault) where TFault : class
        {
            FaultException<TFault> target = new FaultException<TFault>(request, fault);
            Assert.AreEqual(fault, target.Fault);
            Assert.AreEqual(request, target.Request);
            Assert.IsNotNull(target.Message);
            return target;
        }

        /// <summary>Test stub for GetObjectData(SerializationInfo, StreamingContext)</summary>
        [PexGenericArguments(typeof(int))]
        [PexMethod]
        public void FaultExceptionTFaultTest_GetObjectDataTest<TFault>(
            [PexAssumeUnderTest]FaultException<TFault> target,
            SerializationInfo info,
            StreamingContext context
        ) where TFault : class
        {
            target.GetObjectData(info, context);
            Assert.AreEqual(target.Fault, info.GetValue("fault", typeof(TFault)));
        }
    }
}
