using Microsoft.VisualStudio.TestTools.UnitTesting;
using MS.SyncFrame;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ProtoBuf;

namespace MS.SyncFrame.Tests
{
    [TestClass()]
    public class MessageClientTests
    {
        TcpListener server;
        TcpClient client;
        Stream serverStream;
        Stream clientStream;

        [ProtoContract]
        internal class Message
        {
            [ProtoMember(1)]
            internal byte[] Data { get; set; }
        }

        [TestInitialize]
        public void TestInitialize()
        {
            for (;;)
            {
                Thread.Sleep(200);
                try
                {
                    server = new TcpListener(IPAddress.Loopback, 60001);
                    server.Start();
                    client = new TcpClient();
                    client.Connect(new IPEndPoint(IPAddress.Loopback, 60001));
                }
                catch (Exception)
                {
                    continue;
                }

                break;
            }
            serverStream = client.GetStream();
            clientStream = server.AcceptTcpClient().GetStream();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (client != null)
            {
                client.Close();
                client = null;
            }

            if (server != null)
            {
                server.Stop();
                server = null;
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
        [TestProperty("FrameDelay", "10")]
        [TestProperty("NumRequests", "100")]
        public async Task MessageClientTests_CreateClientSessionTest()
        {
            int responseSize = int.Parse((string)TestContext.Properties["ResponseSize"]);
            Random r = new Random();
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (MessageClient client = new MessageClient(clientStream, cts.Token))
            {
                Task listenTask = CreateListenTask(serverStream, responseSize, cts.Token);
                Task sessionTask = client.Open();
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
                    Message responseMessage = await client.SendData(requestMessage)
                                                          .ReceiveData<Message>()
                                                          .Complete();
                    Assert.IsNotNull(responseMessage);
                    Assert.AreEqual(responseSize, responseMessage.Data.Length);
                }

                cts.Cancel();
                try
                {
                    await sessionTask;
                    Assert.Fail();
                }
                catch (OperationCanceledException)
                {

                }
                Assert.IsFalse(client.IsConnectionOpen);
            }
        }

        static async Task CreateListenTask(Stream serverStream, int responseSize, CancellationToken token)
        {
            Random r = new Random();
            using (MessageServer server = new MessageServer(serverStream, token))
            {
                List<Task> sendTasks = new List<Task>();
                Task sessionTask = server.Open();
                Thread.Sleep(1000);

                while (server.IsConnectionOpen)
                {
                    TypedResult<Message> request = await server.ReceiveData<Message>();
                    byte[] responseData = new byte[responseSize];
                    r.NextBytes(responseData);
                    Message m = new Message { Data = responseData };
                    await request.SendData(m);
                }

                try
                {
                    await sessionTask;
                }
                catch (OperationCanceledException)
                {

                }
            }
        }
    }
}