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
    public class MessageServerTests
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
                    server = new TcpListener(IPAddress.Loopback, 60002);
                    server.Start();
                    client = new TcpClient();
                    client.Connect(new IPEndPoint(IPAddress.Loopback, 60002));
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
        public async Task MessageServerTests_CreateServerSessionTest()
        {
            int numRequests = int.Parse((string)TestContext.Properties["NumRequests"]);
            int requestSize = int.Parse((string)TestContext.Properties["RequestSize"]);
            int responseSize = int.Parse((string)TestContext.Properties["ResponseSize"]);
            int frameDelay = int.Parse((string)TestContext.Properties["FrameDelay"]);
            TimeSpan minDelay = TimeSpan.FromMilliseconds(frameDelay);
            Random r = new Random();
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (MessageServer server = new MessageServer(serverStream, cts.Token))
            {
                Task listenTask = CreateTransmitTask(clientStream, minDelay, numRequests, requestSize, cts.Token);
                Task sessionTask = server.Open();
                Assert.IsTrue(server.IsConnectionOpen);

                for (int i = 0; i < numRequests; ++i)
                { 
                    TypedResult<Message> request = await server.ReceiveData<Message>();
                    Assert.AreEqual(requestSize, request.Data.Data.Length);
                    // Do something with the message.
                    byte[] responseData = new byte[responseSize];
                    r.NextBytes(responseData);
                    await request.SendData(new Message { Data = responseData });
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
                Assert.IsFalse(server.IsConnectionOpen);
            }
        }

        internal static async Task CreateTransmitTask(Stream serverStream, TimeSpan frameDelay, int numRequests, int requestSize, CancellationToken token)
        {
                Random r = new Random();

                using (MessageClient client = new MessageClient(serverStream, token))
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

                    await sessionTask;
                }
        }
    }
}