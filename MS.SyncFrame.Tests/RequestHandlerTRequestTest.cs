using System.Threading;
using System.Threading.Tasks;
using System;
using MS.SyncFrame;
using Microsoft.Pex.Framework;
using Microsoft.Pex.Framework.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MS.SyncFrame.Tests
{
    /// <summary>This class contains parameterized unit tests for RequestHandler`1</summary>
    [TestClass]
    [PexClass(typeof(RequestHandler<>))]
    [PexAllowedExceptionFromTypeUnderTest(typeof(ArgumentException), AcceptExceptionSubtypes = true)]
    [PexAllowedExceptionFromTypeUnderTest(typeof(InvalidOperationException))]
    public partial class RequestHandlerTRequestTest
    {

        /// <summary>Test stub for .ctor(Func`1&lt;Task`1&lt;!0&gt;&gt;, Func`2&lt;!0,Task&gt;)</summary>
        [PexGenericArguments(typeof(int))]
        [PexMethod]
        public RequestHandler<TRequest> ConstructorTest<TRequest>(Func<Task<TRequest>> requestHandler, Func<TRequest, Task> responseHandler)
        {
            RequestHandler<TRequest> target = new RequestHandler<TRequest>(requestHandler, responseHandler);
            return target;
        }

        /// <summary>Test stub for .ctor(Func`1&lt;Task`1&lt;!0&gt;&gt;, Func`2&lt;!0,Task&gt;, CancellationToken)</summary>
        [PexGenericArguments(typeof(int))]
        [PexMethod]
        public RequestHandler<TRequest> ConstructorTest01<TRequest>(
            Func<Task<TRequest>> requestHandler,
            Func<TRequest, Task> responseHandler,
            CancellationToken token
        )
        {
            RequestHandler<TRequest> target
               = new RequestHandler<TRequest>(requestHandler, responseHandler, token);
            return target;
        }

        /// <summary>Test stub for Handle()</summary>
        [PexGenericArguments(typeof(int))]
        [PexMethod]
        public Task HandleTest<TRequest>([PexAssumeUnderTest]RequestHandler<TRequest> target)
        {
            Task result = target.Handle();
            return result;
        }
    }
}
