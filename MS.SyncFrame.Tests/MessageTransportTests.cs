using Microsoft.VisualStudio.TestTools.UnitTesting;
using MS.SyncFrame;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ProtoBuf;

namespace MS.SyncFrame.Tests
{
    [TestClass()]
    public class MessageTransportTests
    {
        CancellationTokenSource cts;
        Stream testStream;
        TestTransport outTransport;
        TestTransport inTransport;

        interface ITestService
        {
            Task<Message> LastMessage { get; }

            Task SendData(Message msg);
        }

        class TestServiceHost : ITestService
        {
            Message message;

            public Task<Message> LastMessage
            {
                get
                {
                    return Task.FromResult(message);
                }
            }

            public Task SendData(Message msg)
            {
                return Task.Factory.StartNew(
                    () => message = msg
                );
            }
        }

        class TestTransport : MessageTransport
        {
            public TestTransport(Stream testStream, CancellationToken token)
                : base(testStream, token)
            {
            }

            public CancellationToken Token
            {
                get
                {
                    return base.ConnectionClosedToken;
                }
            }

            public void Sync(bool isOutTransport)
            {
                if (isOutTransport)
                {
                    WriteMessages();
                    ReadMessages();
                }
                else
                {
                    ReadMessages();
                    WriteMessages();
                }
            }
        }
        public TestContext TestContext
        {
            get;
            set;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            testStream = new MemoryStream();
            cts = new CancellationTokenSource();
            outTransport = new TestTransport(testStream, cts.Token);
            inTransport = new TestTransport(testStream, cts.Token);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (outTransport != null)
            {
                outTransport.Dispose();
                outTransport = null;
            }

            if (inTransport != null)
            {
                inTransport.Dispose();
                inTransport = null;
            }

            if (testStream != null)
            {
                testStream.Dispose();
                testStream = null;
            }

            if (cts != null)
            {
                cts.Dispose();
                cts = null;
            }
        }

        [TestMethod()]
        public void TokenTest()
        {
            Assert.AreEqual(cts.Token, outTransport.Token);
            Assert.AreEqual(cts.Token, inTransport.Token);
        }

        [TestMethod()]
        [TestProperty("NumIterations", "1")]
        public void SendRecieveDataTest()
        {
            outTransport.Open().Wait();
            inTransport.Open().Wait();

            int numIterations = int.Parse((string)TestContext.Properties["NumIterations"]);
            for (int i = 0; i < numIterations; ++i)
            {
                Message msg = new Message { Data = i };
                outTransport.SendData(msg).Wait();
                outTransport.Sync(true);
                Task<TypedResult<Message>> inMsg = inTransport.ReceiveData<Message>();
                inTransport.Sync(false);
                Assert.AreEqual(msg.Data, inMsg.Result.Result.Data);
            }
        }

        [TestMethod()]
        [TestProperty("NumIterations", "1")]
        public void SendRecieveMessageTest()
        {
            outTransport.Open().Wait();
            inTransport.Open().Wait();

            int numIterations = int.Parse((string)TestContext.Properties["NumIterations"]);
            for (int i = 0; i < numIterations; ++i)
            {
                Message request = new Message { Data = i };
                Task<TypedResult<Message>> responseTask = outTransport.SendData(request).ReceiveData<Message>();
                outTransport.Sync(true);
                Task requestTask = inTransport.ReceiveData<Message>().SendData(request);
                inTransport.Sync(false);
                requestTask.Wait();
                Message response = responseTask.Result.Result;
                Assert.AreEqual(request.Data, response.Data);
            }
        }

        [TestMethod()]
        public void OnFaultTest()
        {
            outTransport.Open().Wait();
            inTransport.Open().Wait();

            Message request = new Message { Data = -1 };
            Task<Task> faultMessageTask = outTransport.OnFault();
            Task<TypedResult<Message>> responseTask = outTransport.SendData(request).ReceiveData<Message>();
            outTransport.Sync(true);
            Task requestTask = inTransport.ReceiveData<Message>()
                                          .ContinueWith((t) => { throw new FaultException<Message>( t.Result, t.Result.Result); });
            inTransport.Sync(false);
            requestTask.Wait();
            try
            {
                Message response = responseTask.Result.Result;
                Assert.Fail();
            }
            catch (AggregateException ex)
            {
                Assert.IsTrue(ex.InnerException is FaultException<Message>);
                Message result = ((FaultException<Message>)ex.InnerException).Fault;
                Assert.AreEqual(request.Data, result.Data);
            }

            try
            {
                Message response = responseTask.Result.Result;
                Assert.Fail();
            }
            catch (AggregateException ex)
            {
                Assert.IsTrue(ex.InnerException is FaultException<Message>);
                Message result = ((FaultException<Message>)ex.InnerException).Fault;
                Assert.AreEqual(request.Data, result.Data);
            }

            Task faultTask = faultMessageTask.Result;
            try
            {
                faultTask.Wait();
                Assert.Fail();
            }
            catch (AggregateException ex)
            {
                Assert.IsTrue(ex.InnerException is FaultException<Message>);
                Message result = ((FaultException<Message>)ex.InnerException).Fault;
                Assert.AreEqual(request.Data, result.Data);
            }

            Assert.IsFalse(inTransport.IsConnectionOpen);
            Assert.IsFalse(outTransport.IsConnectionOpen);
        }

        [TestMethod()]
        public void CloseTest()
        {
            Assert.IsFalse(outTransport.IsConnectionOpen);
            Assert.IsFalse(inTransport.IsConnectionOpen);
            Task outSessionTask = outTransport.Open();
            Task inSessionTask = inTransport.Open();
            outSessionTask.Wait();
            inSessionTask.Wait();
            Assert.IsTrue(inTransport.IsConnectionOpen);
            Assert.IsTrue(outTransport.IsConnectionOpen);
            cts.Cancel();
            Assert.IsFalse(inTransport.IsConnectionOpen);
            Assert.IsFalse(outTransport.IsConnectionOpen);
        }

        [ProtoContract]
        internal class Message
        {
            [ProtoMember(1)]
            internal int Data { get; set; }
        }
    }
}