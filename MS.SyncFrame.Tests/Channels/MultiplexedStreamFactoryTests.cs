using Microsoft.VisualStudio.TestTools.UnitTesting;
using MS.SyncFrame.Channels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MS.SyncFrame.Channels.Tests
{
    [TestClass()]
    public class MultiplexedStreamFactoryTests
    {
        TcpListener server;
        TcpClient client;
        Stream serverStream;
        Stream clientStream;
        MultiplexedStreamFactory serverFactory;
        MultiplexedStreamFactory clientFactory;
        CancellationTokenSource cts = new CancellationTokenSource();

        [TestInitialize]
        public void TestInitialize()
        {
            server = new TcpListener(IPAddress.Loopback, 60004);
            server.Start();
            client = new TcpClient();
            client.Connect(new IPEndPoint(IPAddress.Loopback, 60004));
            serverStream = client.GetStream();
            clientStream = server.AcceptTcpClient().GetStream();
            serverFactory = new MultiplexedStreamFactory(serverStream, cts.Token);
            clientFactory = new MultiplexedStreamFactory(clientStream, cts.Token);
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

            if (serverFactory != null)
            {
                serverFactory.Dispose();
                serverFactory = null;
            }

            if (clientFactory != null)
            {
                clientFactory.Dispose();
                clientFactory = null;
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

        [TestMethod()]
        [TestProperty("NumIterations", "100")]
        [TestProperty("NumChannels", "4")]
        public async Task MultiplexedStreamFactory_TwoWayTest()
        {
            int numIterations = int.Parse((string)TestContext.Properties["NumIterations"]);
            int numChannels = int.Parse((string)TestContext.Properties["NumChannels"]);
            Task channelTask = ChannelTask();
            List<Task> channelTasks = new List<Task>();
            for (int j = 0; j < numChannels; ++j)
            {
                int channel = j;
                Task clientTask = Task.Run(() =>
                {
                    Stream clientStream = clientFactory.CreateChannel(channel);
                    for (int i = 0; i < numIterations; ++i)
                    {
                        Random r = new Random(channel);
                        byte[] writeByte = BitConverter.GetBytes(r.Next());
                        clientStream.Write(writeByte, 0, writeByte.Length);
                        int expected = r.Next();
                        byte[] readByte = new byte[4];
                        clientStream.Read(readByte, 0, readByte.Length);
                        int actual = BitConverter.ToInt32(readByte, 0);
                        Assert.AreEqual(expected, actual);
                    }
                });
                Task serverTask = Task.Run(() =>
                {
                    Stream serverStream = serverFactory.CreateChannel(channel);
                    for (int i = 0; i < numIterations; ++i)
                    {
                        Random r = new Random(channel);
                        int expected = r.Next();
                        byte[] readByte = new byte[4];
                        serverStream.Read(readByte, 0, readByte.Length);
                        int actual = BitConverter.ToInt32(readByte, 0);
                        Assert.AreEqual(expected, actual);
                        byte[] writeByte = BitConverter.GetBytes(r.Next());
                        serverStream.Write(writeByte, 0, writeByte.Length);
                    }
                });
                channelTasks.Add(serverTask);
                channelTasks.Add(clientTask);
            }

            await Task.Factory.ContinueWhenAll(channelTasks.ToArray(), (t) => { });

            cts.Cancel();
            await channelTask;
            Assert.IsFalse(clientFactory.IsConnectionOpen);
            Assert.IsFalse(serverFactory.IsConnectionOpen);
        }

        [TestMethod()]
        [TestProperty("NumIterations", "100")]
        [TestProperty("NumChannels", "4")]
        public async Task MultiplexedStreamFactory_OneWayTest()
        {
            int numIterations = int.Parse((string)TestContext.Properties["NumIterations"]);
            int numChannels = int.Parse((string)TestContext.Properties["NumChannels"]);
            Task channelTask = ChannelTask();
            List<Task> channelTasks = new List<Task>();
            for (int j = 0; j < numChannels; ++j)
            {
                int channel = j;
                Task clientTask = Task.Run(() =>
                {
                    Random r = new Random(channel);
                    Stream clientStream = clientFactory.CreateChannel(channel);
                    for (int i = 0; i < numIterations; ++i)
                    {
                        byte[] writeByte = BitConverter.GetBytes(r.Next());
                        clientStream.Write(writeByte, 0, writeByte.Length);
                    }
                });
                Task serverTask = Task.Run(() =>
                {
                    Random r = new Random(channel);
                    Stream serverStream = serverFactory.CreateChannel(channel);
                    for (int i = 0; i < numIterations; ++i)
                    {
                        int expected = r.Next();
                        byte[] readByte = new byte[4];
                        serverStream.Read(readByte, 0, readByte.Length);
                        int actual = BitConverter.ToInt32(readByte, 0);
                        Assert.AreEqual(expected, actual);
                    }
                });
                channelTasks.Add(serverTask);
                channelTasks.Add(clientTask);
            }

            await Task.Factory.ContinueWhenAll(channelTasks.ToArray(), (t) => { });

            cts.Cancel();
            await channelTask;
            Assert.IsFalse(clientFactory.IsConnectionOpen);
            Assert.IsFalse(serverFactory.IsConnectionOpen);
        }

        [TestMethod()]
        public async Task MultiplexedStreamFactory_OpenTest()
        {
            Assert.IsFalse(clientFactory.IsConnectionOpen);
            Assert.IsFalse(serverFactory.IsConnectionOpen);
            Task clientFactoryTask = clientFactory.Open();
            Task serverFactoryTask = serverFactory.Open();
            Thread.Sleep(1000);
            Assert.IsTrue(clientFactory.IsConnectionOpen);
            Assert.IsTrue(serverFactory.IsConnectionOpen);
            cts.Cancel();
            try
            {
                await clientFactoryTask;
                Assert.Fail();
            }
            catch (Exception)
            {
            }
            try
            {
                await serverFactoryTask;
                Assert.Fail();
            }
            catch (Exception)
            {
            }
            Assert.IsFalse(clientFactory.IsConnectionOpen);
            Assert.IsFalse(serverFactory.IsConnectionOpen);
        }

        private async Task ChannelTask()
        {
            Task clientFactoryTask = clientFactory.Open();
            Task serverFactoryTask = serverFactory.Open();
            try
            {
                await clientFactoryTask;
                Assert.Fail();
            }
            catch (Exception)
            {
            }
            try
            {
                await serverFactoryTask;
                Assert.Fail();
            }
            catch (Exception)
            {
            }
        }
    }
}