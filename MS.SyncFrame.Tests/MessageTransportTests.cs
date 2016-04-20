using Microsoft.VisualStudio.TestTools.UnitTesting;
using MS.SyncFrame;
using System;
using System.Net;
using System.Net.Sockets;
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
        TcpListener server;
        TcpClient client;
        Stream serverStream;
        Stream clientStream;
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

            public async Task SyncWrite()
            {
                await WriteMessages();
            }

            public async Task SyncRead()
            {
                await ReadMessages();
            }
        }
        public TestContext TestContext
        {
            get;
            set;
        }

        [TestInitialize]
        public void MessageTransportTests_TestInitialize()
        {
            server = new TcpListener(IPAddress.Loopback, 60000);
            server.Start();
            client = new TcpClient();
            client.Connect(new IPEndPoint(IPAddress.Loopback, 60000));
            cts = new CancellationTokenSource();
            serverStream = client.GetStream();
            clientStream = server.AcceptTcpClient().GetStream();
            outTransport = new TestTransport(clientStream, cts.Token);
            inTransport = new TestTransport(serverStream, cts.Token);
        }

        [TestCleanup]
        public void MessageTransportTests_TestCleanup()
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

            if (cts != null)
            {
                cts.Dispose();
                cts = null;
            }
        }

        [TestMethod()]
        public void MessageTransportTests_ProtobufReadWriteTest()
        {
            FrameHeader frameHeader = new FrameHeader();
            frameHeader.MessageSizes = new int[] { 1 };
            Serializer.SerializeWithLengthPrefix(clientStream, frameHeader, PrefixStyle.Base128);
            FrameHeader frameHeader2 = Serializer.DeserializeWithLengthPrefix<FrameHeader>(serverStream, PrefixStyle.Base128);
            Assert.AreEqual(frameHeader.MessageSizes.Length, frameHeader2.MessageSizes.Length);
            Assert.AreEqual(frameHeader.MessageSizes[0], frameHeader2.MessageSizes[0]);
            MessageHeader messageHeader = new MessageHeader();
            messageHeader.DataType = typeof(object);
            messageHeader.Faulted = false;
            messageHeader.RequestId = 2;
            messageHeader.Response = true;
            Serializer.SerializeWithLengthPrefix(clientStream, messageHeader, PrefixStyle.Base128);
            MessageHeader messageHeader2 = Serializer.DeserializeWithLengthPrefix<MessageHeader>(serverStream, PrefixStyle.Base128);
            Assert.AreEqual(messageHeader.DataType, messageHeader2.DataType);
            Assert.AreEqual(messageHeader.Faulted, messageHeader2.Faulted);
            Assert.AreEqual(messageHeader.RequestId, messageHeader2.RequestId);
            Assert.AreEqual(messageHeader.Response, messageHeader2.Response);
        }

        [TestMethod()]
        public void MessageTransportTests_TokenTest()
        {
            Assert.AreEqual(cts.Token, outTransport.Token);
            Assert.AreEqual(cts.Token, inTransport.Token);
        }

        [TestMethod()]
        [TestProperty("NumIterations", "10")]
        public async Task MessageTransportTests_SendRecieveDataTest()
        {
            await outTransport.Open();
            await inTransport.Open();

            int numIterations = int.Parse((string)TestContext.Properties["NumIterations"]);
            for (int i = 0; i < numIterations; ++i)
            {
                Message msg = new Message { Data = i };
                Task<RequestResult> clientRequestTask = outTransport.SendData(msg);
                await StepWrite();
                RequestResult clientRequest = await clientRequestTask;
                TypedResult<Message> serverRequest = await inTransport.ReceiveData<Message>();
                await StepRead();
                Assert.AreEqual(msg.Data, serverRequest.Data.Data);
                Assert.AreEqual(clientRequest.RequestId, serverRequest.RequestId);
            }
        }

        [TestMethod()]
        [TestProperty("NumIterations", "1")]
        public async Task MessageTransportTests_SendRecieveMessageTest()
        {
            await outTransport.Open();
            await inTransport.Open();

            int numIterations = int.Parse((string)TestContext.Properties["NumIterations"]);
            for (int i = 10; i < numIterations; ++i)
            {
                Message request = new Message { Data = i };
                Task<RequestResult> clientRequest = outTransport.SendData(request);
                await StepWrite();
                await clientRequest;
                TypedResult<Message> serverRequest = await inTransport.ReceiveData<Message>();
                Task<Result> serverResponseTask = serverRequest.SendData(request);
                await StepRead();
                Result serverResponse = await serverResponseTask;
                TypedResult<Message> response = await clientRequest.ReceiveData<Message>();
                Assert.AreEqual(request.Data, response.Data.Data);
                Assert.AreEqual(clientRequest.Result.RequestId, serverRequest.RequestId);
                Assert.AreEqual(clientRequest.Result.RequestId, response.RequestId);
            }
        }

        [TestMethod()]
        public async Task MessageTransportTests_OnFaultTest()
        {
            await outTransport.Open();
            await inTransport.Open();

            Message request = new Message { Data = -1 };
            Task<RequestResult> clientRequest = outTransport.SendData(request);
            await StepWrite();
            await clientRequest;
            TypedResult<Message> serverRequest = await inTransport.ReceiveData<Message>();
            Task<FaultException<Message>> faultExTask = serverRequest.SendFault(request);
            await StepRead();
            FaultException<Message> faultEx = await faultExTask;
            Assert.IsNotNull(faultEx);

            try
            {
                await clientRequest.ReceiveData<Message>();
                Assert.Fail();
            }
            catch (FaultException<Message> ex)
            {
                Message result = ex.Fault;
                Assert.AreEqual(request.Data, result.Data);
            }

            try
            {
                await outTransport.OnFault();
                Assert.Fail();
            }
            catch (FaultException<Message> ex)
            {
                Message result = ex.Fault;
                Assert.AreEqual(request.Data, result.Data);
            }

            try
            {
                await inTransport.OnFault();
                Assert.Fail();
            }
            catch (FaultException<Message> ex)
            {
                Message result = ex.Fault;
                Assert.AreEqual(request.Data, result.Data);
            }

            Assert.IsFalse(inTransport.IsConnectionOpen);
            Assert.IsFalse(outTransport.IsConnectionOpen);
        }

        [TestMethod()]
        public async Task MessageTransportTests_CloseTest()
        {
            Assert.IsFalse(outTransport.IsConnectionOpen);
            Assert.IsFalse(inTransport.IsConnectionOpen);
            Task outSessionTask = outTransport.Open();
            Task inSessionTask = inTransport.Open();
            await outSessionTask;
            await inSessionTask;
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

        private async Task StepWrite()
        {
            try
            {
                await outTransport.SyncWrite();
            }
            catch (Exception)
            {

            }
            try
            {
                await inTransport.SyncRead();
            }
            catch (Exception)
            {

            }
        }

        private async Task StepRead()
        {
            try
            {
                await inTransport.SyncWrite();
            }
            catch (Exception)
            {

            }
            try
            {
                await outTransport.SyncRead();
            }
            catch (Exception)
            {

            }
        }
    }
}