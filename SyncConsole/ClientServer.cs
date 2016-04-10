//-----------------------------------------------------------------------
// <copyright file="ClientServer.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace SyncConsole
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using MS.SyncFrame;
    using ProtoBuf;

    internal static class ClientServer
    {
        internal static Task CreateListenTask(Stream serverStream, int responseSize)
        {
            return Task.Factory.StartNew(async () =>
            {
                Random r = new Random();
                using (CancellationTokenSource cts = new CancellationTokenSource())
                using (MessageServer server = new MessageServer(serverStream, cts.Token))
                {
                    Task sessionTask = server.Open();

                    while (server.IsConnectionOpen)
                    {
                        byte[] responseData = new byte[responseSize];
                        r.NextBytes(responseData);
                        await server.ReceiveData<Message>()
                                    .SendData(new Message { Data = responseData })
                                    .Complete();
                    }

                    cts.Cancel();
                    await sessionTask;
                }
            });
        }

        internal static Task CreateTransmitTask(NetworkStream clientStream, TimeSpan frameDelay, int numRequests, int requestSize)
        {
            return Task.Factory.StartNew(async () =>
            {
                Random r = new Random();
                using (CancellationTokenSource cts = new CancellationTokenSource())
                using (MessageClient client = new MessageClient(clientStream, cts.Token))
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

        [ProtoContract]
        internal class Message
        {
            [ProtoMember(1)]
            internal byte[] Data { get; set; }
        }
    }
}
