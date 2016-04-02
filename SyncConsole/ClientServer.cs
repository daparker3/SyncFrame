//-----------------------------------------------------------------------
// <copyright file="ClientServer.cs" company="MS">
//     Copyright (c) 2016 MS.
// </copyright>
//-----------------------------------------------------------------------

namespace SyncConsole
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using MS.SyncFrame;
    using ProtoBuf;

    internal static class ClientServer
    {
        internal static Task CreateListenTask(Stream clientStream, int responseSize)
        {
            return Task.Factory.StartNew(async () =>
            {
                Random r = new Random();
                MessageServer server = await MessageServer.CreateServerSession(clientStream);
                await server.Listen();

                while (server.IsConnectionOpen)
                {
                    byte[] responseData = new byte[responseSize];
                    r.NextBytes(responseData);
                    await server.RecieveData<Message>()
                                .ContinueWith((t) => server.SendData(new Message { Data = responseData }))
                                .Unwrap();
                }

                await server.Close();
            });
        }

        internal static Task CreateTransmitTask(NetworkStream serverStream, TimeSpan frameDelay, int numRequests, int requestSize)
        {
            return Task.Factory.StartNew(async () =>
            {
                Random r = new Random();
                MessageClient client = await MessageClient.CreateClientSession(serverStream);
                client.MinDelay = frameDelay;

                for (int i = 0; i < numRequests; ++i)
                {
                    // Do something with the message.
                    byte[] requestData = new byte[requestSize];
                    r.NextBytes(requestData);
                    Message requestMessage = new Message { Data = requestData };
                    Message responseMessage = client.SendData(requestMessage)
                                                    .ContinueWith((t) => client.RecieveData<Message>())
                                                    .Unwrap().Result;
                }
            });
        }

        [ProtoContract]
        internal class Message
        {
            [ProtoMember(1)]
            internal byte[] Data { get; set; }
        }
    }
}
