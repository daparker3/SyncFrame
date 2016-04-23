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
        internal static async Task CreateListenTask(Stream serverStream, ListenArgs listenArgs)
        {
            Random r = new Random();
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (MessageServer server = new MessageServer(serverStream, cts.Token))
            {
                Task sessionTask = server.Open();

                while (server.IsConnectionOpen)
                {
                    byte[] responseData = new byte[listenArgs.ResponseSize];
                    r.NextBytes(responseData);
                    TypedResult<Message> request = await server.ReceiveData<Message>();
                    await request.SendData(new Message { Data = responseData });
                }

                cts.Cancel();
                await sessionTask;
            }
        }

        internal static async Task<double> CreateTransmitTask(NetworkStream clientStream, TransmitArgs transmitArgs)
        {
            if (transmitArgs.NumIterations == 0)
            {
                return 0.0;
            }

            Random r = new Random();
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (MessageClient client = new MessageClient(clientStream, cts.Token))
            {
                double latency = 0.0;
                client.MinDelay = transmitArgs.FrameDelay;
                Task sessionTask = client.Open();

                for (int j = 0; j < transmitArgs.NumIterations; ++j)
                {
                    List<Task<RequestResult>> requests = new List<Task<RequestResult>>(transmitArgs.NumRequests);
                    for (int i = 0; i < transmitArgs.NumRequests; ++i)
                    {
                        // Do something with the message.
                        byte[] requestData = new byte[transmitArgs.RequestSize];
                        r.NextBytes(requestData);
                        Message requestMessage = new Message { Data = requestData };
                        requests.Add(client.SendData(requestMessage));
                    }

                    for (int i = 0; i < transmitArgs.NumRequests; ++i)
                    {
                        await requests[i].ReceiveData<Message>().Complete();
                    }

                    latency += (await client.GetLatencySpan()).TotalMilliseconds;
                }

                cts.Cancel();
                return latency / (double)transmitArgs.NumIterations;
            }
        }

        private static async Task<double> SumUnusedLatency(MessageClient client, CancellationToken token)
        {
            double latency = 0.0;
            while (!token.IsCancellationRequested)
            {
                latency += (await client.GetLatencySpan()).TotalMilliseconds;
            }

            return latency;
        }

        [ProtoContract]
        internal class Message
        {
            [ProtoMember(1)]
            internal byte[] Data { get; set; }
        }
    }
}
