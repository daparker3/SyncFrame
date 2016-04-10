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
    public class MessageClientTests
    {
        Stream sessionStream;

        [ProtoContract]
        internal class Message
        {
            [ProtoMember(1)]
            internal byte[] Data { get; set; }
        }

        [TestInitialize]
        public void TestInitialize()
        {
            sessionStream = new MemoryStream();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (sessionStream != null)
            {
                sessionStream.Close();
                sessionStream.Dispose();
                sessionStream = null;
            }
        }

        public TestContext TestContext
        {
            get;
            set;
        }

        [TestMethod()]
        [TestProperty("RequestSize", "100")]
        [TestProperty("ResponseSize", "100")]
        [TestProperty("FrameDelay", "100")]
        [TestProperty("NumRequests", "100")]
        public void MessageClientTests_CreateClientSessionTest()
        {
            int responseSize = int.Parse((string)TestContext.Properties["ResponseSize"]);
            Task listenTask = CreateListenTask(sessionStream, responseSize);
            Random r = new Random();
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (MessageClient client = new MessageClient(sessionStream, cts.Token))
            {
                Task sessionTask = client.Open();
                Thread.Sleep(1000);
                Assert.IsTrue(client.IsConnectionOpen);

                int frameDelay = int.Parse((string)TestContext.Properties["FrameDelay"]);
                TimeSpan minDelay = TimeSpan.FromMilliseconds(frameDelay);
                client.MinDelay = minDelay;
                Assert.AreEqual(minDelay, client.MinDelay);

                int numRequests = int.Parse((string)TestContext.Properties["NumRequests"]);
                int requestSize = int.Parse((string)TestContext.Properties["RequestSize"]);
                for (int i = 0; i < numRequests; ++i)
                {
                    // Do something with the message.
                    byte[] requestData = new byte[requestSize];
                    r.NextBytes(requestData);
                    Message requestMessage = new Message { Data = requestData };
                    Message responseMessage = client.SendData(requestMessage)
                                                    .ReceiveData<Message>()
                                                    .Complete().Result;
                    Assert.IsNotNull(responseMessage);
                    Assert.AreEqual(responseSize, responseMessage.Data.Length);
                }

                cts.Cancel();
                sessionTask.Wait();
                Assert.IsFalse(client.IsConnectionOpen);
            }
            listenTask.Wait();
        }

        static Task CreateListenTask(Stream clientStream, int responseSize)
        {
            return Task.Factory.StartNew(async () =>
            {
                Random r = new Random();
                using (CancellationTokenSource cts = new CancellationTokenSource())
                using (MessageServer server = new MessageServer(clientStream, cts.Token))
                {
                    List<Task> sendTasks = new List<Task>();
                    Task sessionTask = server.Open();

                    while (server.IsConnectionOpen)
                    {
                        await server.ReceiveData<Message>()
                            .ContinueWith((t) =>
                            {
                                byte[] responseData = new byte[responseSize];
                                r.NextBytes(responseData);
                                Message m = new Message { Data = responseData };
                                return t.SendData(m);
                            }).Unwrap()
                            .Complete();
                    }

                    cts.Cancel();
                    await sessionTask;
                }
            });
        }
    }
}