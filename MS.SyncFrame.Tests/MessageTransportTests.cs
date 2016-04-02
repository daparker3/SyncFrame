using Microsoft.VisualStudio.TestTools.UnitTesting;
using MS.SyncFrame;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace MS.SyncFrame.Tests
{
    [TestClass()]
    public class MessageTransportTests
    {
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
            public TestTransport(Stream testStream)
                : base(testStream)
            {
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
            outTransport = new TestTransport(testStream);
            inTransport = new TestTransport(testStream);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (outTransport != null)
            {
                outTransport.Close();
                outTransport.Dispose();
                outTransport = null;
            }

            if (inTransport != null)
            {
                inTransport.Dispose();
                inTransport.Dispose();
                inTransport = null;
            }

            if (testStream != null)
            {
                testStream.Dispose();
                testStream = null;
            }
        }

        [TestMethod()]
        [TestProperty("NumIterations", "1")]
        public void SendRecieveDataTest()
        {
            int numIterations = int.Parse((string)TestContext.Properties["NumIterations"]);
            for (int i = 0; i < numIterations; ++i)
            {
                Message msg = new Message { Data = i };
                outTransport.SendData(msg);
                outTransport.Sync(true);
                Task<Message> inMsg = inTransport.RecieveData<Message>();
                inTransport.Sync(false);
                Assert.AreEqual(msg.Data, inMsg.Result.Data);
            }
        }

        [TestMethod()]
        [TestProperty("NumIterations", "1")]
        public void SendRecieveMessageTest()
        {
            int numIterations = int.Parse((string)TestContext.Properties["NumIterations"]);
            for (int i = 0; i < numIterations; ++i)
            {
                Message request = new Message { Data = i };
                Task<Message> responseTask = outTransport.SendData(request)
                                                         .ContinueWith((t) => outTransport.RecieveData<Message>())
                                                         .Unwrap();
                outTransport.Sync(true);
                Task requestTask = inTransport.RecieveData<Message>()
                                              .ContinueWith((t) => inTransport.SendData(t.Result));
                inTransport.Sync(false);
                requestTask.Wait();
                Message response = responseTask.Result;
                Assert.AreEqual(request.Data, response.Data);
            }
        }

        [TestMethod()]
        [TestProperty("NumIterations", "1")]
        public void ConnectHostServiceTest()
        {
            int numIterations = int.Parse((string)TestContext.Properties["NumIterations"]);
            TestServiceHost serviceHost = new TestServiceHost();
            Uri serviceUri = new Uri("com.MS.SyncFrame.Tests/TestServiceHost");

            Task serviceHostedTask = inTransport.HostService(serviceHost, serviceUri);
            Task<ITestService> testService = outTransport.ConnectService<ITestService>(serviceUri);
            outTransport.Sync(true);
            inTransport.Sync(false);
            serviceHostedTask.Wait();
            ITestService testServiceProxy = testService.Result;

            for (int i = 0; i < numIterations; ++i)
            {
                Message msg = new Message { Data = i };
                testServiceProxy.SendData(msg)
                                .Wait();
                Message actual = testServiceProxy.LastMessage.Result;
                Assert.AreEqual(msg.Data, actual.Data);
            }
        }

        [TestMethod()]
        public void OnFaultTest()
        {
            Message request = new Message { Data = -1 };
            Task<Message> faultMessageTask = outTransport.OnFault<Message>();
            Task<Message> responseTask = outTransport.SendData(request)
                                                        .ContinueWith((t) => outTransport.RecieveData<Message>())
                                                        .Unwrap();
            outTransport.Sync(true);
            Task requestTask = inTransport.RecieveData<Message>()
                                          .ContinueWith((t) => { throw new FaultException<Message>(t.Result); });
            inTransport.Sync(false);
            requestTask.Wait();
            try
            {
                Message response = responseTask.Result;
                Assert.Fail();
            }
            catch (AggregateException ex)
            {
                Assert.IsTrue(ex.InnerException is FaultException<Message>);
                Message result = ((FaultException<Message>)ex.InnerException).Fault;
                Assert.AreEqual(request.Data, result.Data);
            }

            Message fault = faultMessageTask.Result;
            Assert.AreEqual(request.Data, fault.Data);
            Assert.IsFalse(inTransport.IsConnectionOpen);
            Assert.IsFalse(outTransport.IsConnectionOpen);
        }

        [TestMethod()]
        public void CloseTest()
        {
            Assert.IsTrue(inTransport.IsConnectionOpen);
            Assert.IsTrue(outTransport.IsConnectionOpen);
            inTransport.Close();
            outTransport.Close();
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