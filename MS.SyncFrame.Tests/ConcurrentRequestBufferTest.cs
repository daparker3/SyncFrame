using System.Collections.Generic;
using System;
using MS.SyncFrame;
using Microsoft.Pex.Framework;
using Microsoft.Pex.Framework.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MS.SyncFrame.Tests
{
    /// <summary>This class contains parameterized unit tests for ConcurrentRequestBuffer</summary>
    [TestClass]
    [PexClass(typeof(ConcurrentRequestBuffer))]
    [PexAllowedExceptionFromTypeUnderTest(typeof(ArgumentException), AcceptExceptionSubtypes = true)]
    [PexAllowedExceptionFromTypeUnderTest(typeof(InvalidOperationException))]
    public partial class ConcurrentRequestBufferTest
    {

        /// <summary>Test stub for .ctor()</summary>
        [PexMethod]
        internal ConcurrentRequestBuffer ConstructorTest()
        {
            ConcurrentRequestBuffer target = new ConcurrentRequestBuffer();
            Assert.AreEqual(0, target.Count);
            return target;
        }

        /// <summary>Test stub for CancelRequests()</summary>
        [PexMethod]
        internal void CancelRequestsTest([PexAssumeUnderTest]ConcurrentRequestBuffer target)
        {
            target.CancelRequests();
            Assert.AreEqual(0, target.Count);
        }

        /// <summary>Test stub for DequeueRequests()</summary>
        [PexMethod]
        internal IEnumerable<QueuedRequestChunk> DequeueRequestsTest([PexAssumeUnderTest]ConcurrentRequestBuffer target)
        {
            IEnumerable<QueuedRequestChunk> result = target.DequeueRequests();
            int originalCount = target.Count;
            foreach (QueuedRequestChunk chunk in result)
            {
                Assert.IsNotNull(chunk);
                Assert.AreEqual(originalCount - 1, target.Count);
                originalCount = target.Count;
            }
            return result;
        }

        /// <summary>Test stub for QueueRequest(QueuedRequestChunk)</summary>
        [PexMethod]
        internal void QueueRequestTest([PexAssumeUnderTest]ConcurrentRequestBuffer target, QueuedRequestChunk item)
        {
            int originalCount = target.Count;
            target.QueueRequest(item);
            Assert.AreEqual(originalCount + 1, target.Count);
        }

        /// <summary>Test stub for RequeueRequest(QueuedRequestChunk)</summary>
        [PexMethod]
        internal void RequeueRequestTest([PexAssumeUnderTest]ConcurrentRequestBuffer target, QueuedRequestChunk item)
        {
            IEnumerable<QueuedRequestChunk> result = target.DequeueRequests();
            int originalCount = target.Count;
            foreach (QueuedRequestChunk chunk in result)
            {
                target.RequeueRequest(item);
                Assert.AreEqual(target.Count, originalCount);
            }
        }

        /// <summary>Test stub for get_Count()</summary>
        [PexMethod]
        internal int CountGetTest([PexAssumeUnderTest]ConcurrentRequestBuffer target)
        {
            int result = target.Count;
            Assert.IsTrue(result >= 0);
            return result;
        }
    }
}
