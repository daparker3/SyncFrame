//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="MS">
//     Copyright (c) 2016 MS.
// </copyright>
//-----------------------------------------------------------------------

namespace SyncConsole
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Sockets;

    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Usage();
            }

            try
            {
                if (args[0] == "listen")
                {
                    Listen(ParseEp(args[1]), int.Parse(args[2]));
                }
                else if (args[0] == "transmit")
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    Transmit(ParseEp(args[1]), TimeSpan.FromMilliseconds(int.Parse(args[2])), int.Parse(args[3]), int.Parse(args[4]));
                    Console.Out.WriteLine("Elapsed: {0}ms", sw.Elapsed.TotalMilliseconds);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Caught an unhandled exception: {0}", ex.Message);
            }
        }

        private static void Listen(IPEndPoint localEp, int responseSize)
        {
            TcpListener listener = new TcpListener(localEp);
            listener.Start();
            for (;;)
            {
                TcpClient client = listener.AcceptTcpClient();
                try
                {
                    ClientServer.CreateListenTask(client.GetStream(), responseSize)
                                .Wait();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Client closed: {0}", ex.ToString());
                }
                finally
                {
                    client.Close();
                }
            }
        }

        private static void Transmit(IPEndPoint remoteEp, TimeSpan frameDelay, int numRequests, int requestSize)
        {
            TcpClient client = new TcpClient();
            try
            {
                Random r = new Random();
                byte[] buf = new byte[requestSize];

                client.Connect(remoteEp);

                ClientServer.CreateTransmitTask(client.GetStream(), frameDelay, numRequests, requestSize)
                            .Wait();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Client closed: {0}", ex.ToString());
            }
            finally
            {
                client.Close();
            }
        }

        private static IPEndPoint ParseEp(string remoteAddr)
        {
            string[] strParams = remoteAddr.Split(':');
            return new IPEndPoint(IPAddress.Parse(strParams[0]), int.Parse(strParams[1]));
        }

        private static void Usage()
        {
            Console.Out.WriteLine("Usage: ClientServer.exe listen <ip-address>:<port> <response-size>");
            Console.Out.WriteLine("-or-");
            Console.Out.WriteLine("Usage: ClientServer.exe transmit <ip-address>:<port> <frame-delay-ms> <num-messages> <request-size> ");
            Console.Out.WriteLine("Simple SyncFrame session simulator.");
        }
    }
}
