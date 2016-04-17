using System.Threading;
using System.Threading.Tasks;
using System;
using MS.SyncFrame;
using Microsoft.Pex.Framework;
using Microsoft.Pex.Framework.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MS.SyncFrame.Tests
{
    /// <summary>This class contains parameterized unit tests for ConcurrentResponseBuffer</summary>
    [TestClass]
    [PexClass(typeof(ConcurrentResponseBuffer))]
    [PexAllowedExceptionFromTypeUnderTest(typeof(ArgumentException), AcceptExceptionSubtypes = true)]
    [PexAllowedExceptionFromTypeUnderTest(typeof(InvalidOperationException))]
    public partial class ConcurrentResponseBufferTest
    {

        /// <summary>Test stub for .ctor()</summary>
        [PexMethod]
        internal ConcurrentResponseBuffer ConstructorTest()
        {
            ConcurrentResponseBuffer target = new ConcurrentResponseBuffer();
            Assert.AreEqual(0, target.Count);
            return target;
        }

        /// <summary>Test stub for CancelResponses()</summary>
        [PexMethod]
        internal void CancelResponsesTest([PexAssumeUnderTest]ConcurrentResponseBuffer target)
        {
            target.CancelResponses();
            Assert.AreEqual(0, target.Count);
        }

        /// <summary>Test stub for Dispose()</summary>
        [PexMethod]
        internal void DisposeTest([PexAssumeUnderTest]ConcurrentResponseBuffer target)
        {
            target.Dispose();
        }

        /// <summary>Test stub for TakeNextResponse(Type, CancellationToken)</summary>
        [PexMethod]
        internal Task<QueuedResponseChunk> TakeNextResponseTest(
            [PexAssumeUnderTest]ConcurrentResponseBuffer target,
            Type responseType,
            CancellationToken responseCanceledToken
        )
        {
            Task<QueuedResponseChunk> result = target.DequeueResponse(responseType, responseCanceledToken);
            Assert.IsTrue(target.BufferUse <= target.BufferSize);
            Assert.IsTrue(target.Count >= 0);
            return result;
        }

        /// <summary>Test stub for get_BufferSize()</summary>
        [PexMethod]
        internal int BufferSizeGetTest([PexAssumeUnderTest]ConcurrentResponseBuffer target)
        {
            int result = target.BufferSize;
            Assert.IsTrue(target.BufferUse <= target.BufferSize);
            Assert.IsTrue(target.BufferSize > 0);
            return result;
        }

        /// <summary>Test stub for get_Count()</summary>
        [PexMethod]
        internal int CountGetTest([PexAssumeUnderTest]ConcurrentResponseBuffer target)
        {
            int result = target.Count;
            Assert.IsTrue(target.Count > 0);
            return result;
        }

        /// <summary>Test stub for set_BufferSize(Int64)</summary>
        [PexMethod]
        internal void BufferSizeSetTest([PexAssumeUnderTest]ConcurrentResponseBuffer target, int value)
        {
            target.BufferSize = value;
            Assert.AreEqual(value, target.BufferSize);
        }
    }
}
