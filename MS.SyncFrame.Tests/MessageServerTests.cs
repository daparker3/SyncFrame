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
    public class MessageServerTests
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
        public void MessageServerTests_CreateServerSessionTest()
        {
            int numRequests = int.Parse((string)TestContext.Properties["NumRequests"]);
            int requestSize = int.Parse((string)TestContext.Properties["RequestSize"]);
            int responseSize = int.Parse((string)TestContext.Properties["ResponseSize"]);
            int frameDelay = int.Parse((string)TestContext.Properties["FrameDelay"]);
            TimeSpan minDelay = TimeSpan.FromMilliseconds(frameDelay);
            Task listenTask = CreateTransmitTask(sessionStream, minDelay, numRequests, requestSize);
            Random r = new Random();
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (MessageServer server = new MessageServer(sessionStream, cts.Token))
            {
                Task sessionTask = server.Open();
                Assert.IsTrue(server.IsConnectionOpen);

                while (server.IsConnectionOpen)
                {
                    server.ReceiveData<Message>()
                        .ContinueWith((t) =>
                        {
                            Assert.AreEqual(requestSize, t.Result.Result.Data.Length);
                            // Do something with the message.
                            byte[] responseData = new byte[responseSize];
                            r.NextBytes(responseData);
                            return t.SendData(new Message { Data = responseData });
                        }).Unwrap().Complete().Wait();
                }

                cts.Cancel();
                sessionTask.Wait();
                Assert.IsFalse(server.IsConnectionOpen);
            }
            listenTask.Wait();
        }

        internal static Task CreateTransmitTask(Stream serverStream, TimeSpan frameDelay, int numRequests, int requestSize)
        {
            return Task.Factory.StartNew(async () =>
            {
                Random r = new Random();

                using (CancellationTokenSource cts = new CancellationTokenSource())
                using (MessageClient client = new MessageClient(serverStream, cts.Token))
                {
                    client.MinDelay = frameDelay;
                    Task sessionTask = client.Open();

                    for (int i = 0; i < numRequests; ++i)
                    {
                        // Do something with the message.
                        byte[] requestData = new byte[requestSize];
                        r.NextBytes(requestData);
                        Message requestMessage = new Message { Data = requestData };
                        Message responseMessage = await client.SendData(requestMessage)
                                                        .ReceiveData<Message>()
                                                        .Complete();
                    }

                    cts.Cancel();
                    await sessionTask;
                }
            });
        }
    }
}