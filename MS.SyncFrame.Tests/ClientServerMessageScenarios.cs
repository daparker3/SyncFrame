using Microsoft.VisualStudio.TestTools.UnitTesting;
using MS.SyncFrame;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ProtoBuf;

namespace MS.SyncFrame.Tests
{
    [TestClass]
    public class ClientServerMessageScenarios
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        TcpListener server;
        TcpClient client;
        Stream serverStream;
        Stream clientStream;
        MessageClient clientTransport;
        MessageTransport serverTransport;
        Task clientListener;
        Task serverListener;

        [TestInitialize]
        public void TestInitialize()
        {
            server = new TcpListener(IPAddress.Loopback, 60003);
            server.Start();
            client = new TcpClient();
            client.Connect(new IPEndPoint(IPAddress.Loopback, 60003));
            serverStream = client.GetStream();
            clientStream = server.AcceptTcpClient().GetStream();
            serverTransport = new MessageServer(serverStream, cts.Token);
            serverListener = serverTransport.Open();
            clientTransport = new MessageClient(clientStream, cts.Token);
            clientListener = clientTransport.Open();
            clientTransport.MinDelay = TimeSpan.FromMilliseconds(10);
            serverTransport.MaxFrameSize = clientTransport.MaxFrameSize = 12;
        }

        [TestCleanup]
        public void TestCleanup()
        {
            cts.Cancel();

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

            if (clientStream != null)
            {
                clientStream.Close();
                clientStream = null;
            }

            if (serverStream != null)
            {
                serverStream.Close();
                serverStream = null;
            }

            try
            {
                clientListener.Wait();
            }
            catch (Exception)
            {

            }

            try
            {
                serverListener.Wait();
            }
            catch (Exception)
            {

            }

            if (cts != null)
            {
                cts.Dispose();
                cts = null;
            }
        }

        public TestContext TestContext
        {
            get;
            set;
        }

        [TestMethod]
        [TestProperty("NumIterations", "100")]
        public async Task ClientServerMessageScenario_ClientSendsAMessageAndRecievesAResponse()
        {
            int numIterations = int.Parse((string)TestContext.Properties["NumIterations"]);
            for (int i = 0; i < numIterations; ++i)
            {
                Task clientTask = Task.Run(async () =>
                {
                    Message2 msg = await clientTransport.SendData(new Message1 { Data = i })
                                                        .ReceiveData<Message2>().Complete();
                    Assert.AreEqual(msg.Data, i);
                });

                Task serverTask = Task.Run(async () =>
                {
                    TypedResult<Message1> request = await serverTransport.ReceiveData<Message1>();
                    Assert.AreEqual(request.Data.Data, i);
                    await request.SendData(new Message2 { Data = i });
                });

                await clientTask;
                await serverTask;
            }
        }

        [TestMethod]
        [TestProperty("NumIterations", "100")]
        public async Task ClientServerMessageScenario_ServerSendsABroadcastMessageToClient()
        {
            int numIterations = int.Parse((string)TestContext.Properties["NumIterations"]);
            for (int i = 0; i < numIterations; ++i)
            {
                Task clientTask = Task.Run(async () =>
                {
                    Message3 msg = await clientTransport.ReceiveData<Message3>().Complete();
                    Assert.AreEqual(msg.Data, i);
                });

                Task serverTask = Task.Run(async () =>
                {
                    await serverTransport.SendData(new Message3 { Data = i });
                });

                await clientTask;
                await serverTask;
            }
        }

        [TestMethod]
        [TestProperty("NumIterations", "100")]
        public async Task ClientServerMessageScenario_ClientSendsABroadcastMessageToServer()
        {
            int numIterations = int.Parse((string)TestContext.Properties["NumIterations"]);
            for (int i = 0; i < numIterations; ++i)
            {
                Task clientTask = Task.Run(async () =>
                {
                    await clientTransport.SendData(new Message4 { Data = i });
                });

                Task serverTask = Task.Run(async () =>
                {
                    Message4 msg = await serverTransport.ReceiveData<Message4>().Complete();
                    Assert.AreEqual(msg.Data, i);
                });

                await clientTask;
                await serverTask;
            }
        }

        [TestMethod]
        [TestProperty("NumIterations", "100")]
        public async Task ClientServerMessageScenario_ServerSendsAMessageAndRecievesAResponse()
        {
            int numIterations = int.Parse((string)TestContext.Properties["NumIterations"]);
            for (int i = 0; i < numIterations; ++i)
            {
                Task clientTask = Task.Run(async () =>
                {
                    TypedResult<Message1> request = await clientTransport.ReceiveData<Message1>();
                    Assert.AreEqual(request.Data.Data, i);
                    await request.SendData(new Message2 { Data = i });
                });

                Task serverTask = Task.Run(async () =>
                {
                    Message2 msg = await serverTransport.SendData(new Message1 { Data = i })
                                                        .ReceiveData<Message2>().Complete();
                    Assert.AreEqual(msg.Data, i);
                });

                await clientTask;
                await serverTask;
            }
        }

        [TestMethod]
        [TestProperty("NumIterations", "3")]
        public async Task ClientServerMessageScenario_ClientAndServerPerformMixedWorkload()
        {
            int numIterations = int.Parse((string)TestContext.Properties["NumIterations"]);
            for (int i = 0; i < numIterations; ++i)
            {
                Task[] mixedWorkload = new Task[]
                {
                    this.ClientServerMessageScenario_ClientSendsABroadcastMessageToServer(),
                    this.ClientServerMessageScenario_ClientSendsAMessageAndRecievesAResponse(),
                    this.ClientServerMessageScenario_ServerSendsABroadcastMessageToClient(),
                    this.ClientServerMessageScenario_ServerSendsAMessageAndRecievesAResponse()
                };

                foreach (Task t in mixedWorkload)
                {
                    await t;
                }
            }
        }

        [TestMethod]
        public async Task ClientServerMessageScenario_ClientDisconnectsFromServer()
        {
            client.Close();
            try
            {
                await clientListener;
            }
            catch (Exception)
            {
                
            }
            Assert.IsFalse(clientTransport.IsConnectionOpen);
            try
            {
                await serverListener;
            }
            catch (Exception)
            {

            }
            Assert.IsFalse(serverTransport.IsConnectionOpen);
        }

        [ProtoContract]
        class Message1
        {
            [ProtoMember(1)]
            public int Data { get; set; }
        }


        [ProtoContract]
        class Message2
        {
            [ProtoMember(1)]
            public int Data { get; set; }
        }

        [ProtoContract]
        class Message3
        {
            [ProtoMember(1)]
            public int Data { get; set; }
        }

        [ProtoContract]
        class Message4
        {
            [ProtoMember(1)]
            public int Data { get; set; }
        }
    }
}
