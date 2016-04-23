//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------

namespace SyncConsole
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;

    internal class Program
    {
        private static TransmitArgs transmitArgs = new TransmitArgs
        {
            FrameDelay = TimeSpan.FromMilliseconds(10),
            NumIterations = 10,
            NumRequests = 100,
            RequestSize = 100
        };

        private static ListenArgs listenArgs = new ListenArgs
        {
            ResponseSize = 100
        };

        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Usage();
                return;
            }

            try
            {
                if (args.Length > 2)
                {
                    ParseArgs(args.Skip(2).ToArray());
                }

                if (args[0] == "listen")
                {
                    Listen(ParseEp(args[1]), listenArgs);
                }
                else if (args[0] == "transmit")
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    double unused = Transmit(ParseEp(args[1]), transmitArgs);
                    Console.Out.WriteLine("Elapsed: {0}ms  Unused: {1}ms", sw.Elapsed.TotalMilliseconds, unused);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Caught an unhandled exception: {0}", ex.Message);
            }
        }

        private static void Listen(IPEndPoint localEp, ListenArgs listenArgs)
        {
            TcpListener listener = new TcpListener(localEp);
            listener.Start();
            for (;;)
            {
                Console.Out.WriteLine("Listening.");
                TcpClient client = listener.AcceptTcpClient();
                try
                {
                    ClientServer.CreateListenTask(client.GetStream(), listenArgs)
                                .Wait();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.Message);
                    if (ex.InnerException != null)
                    {
                        Console.Error.WriteLine(ex.InnerException.Message);
                    }
                }
                finally
                {
                    client.Close();
                }
            }
        }

        private static double Transmit(IPEndPoint remoteEp, TransmitArgs transmitArgs)
        {
            TcpClient client = new TcpClient();
            try
            {
                Random r = new Random();
                client.Connect(remoteEp);
                return ClientServer.CreateTransmitTask(client.GetStream(), transmitArgs).Result;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Client closed: {0}", ex.ToString());
                throw;
            }
            finally
            {
                client.Close();
            }
        }

        private static void ParseArgs(string[] args)
        { 
            if (args.Length % 2 != 0)
            {
                throw new InvalidOperationException("Incorrect number of arguments.");
            }

            for (int i = 0; i < args.Length; i += 2)
            {
                string value = args[i + 1];
                switch (args[i].ToUpperInvariant())
                {
                    case "/REQUESTSIZE":
                        transmitArgs.RequestSize = int.Parse(value);
                        break;
                    case "/RESPONSESIZE":
                        listenArgs.ResponseSize = int.Parse(value);
                        break;
                    case "/NUMREQUESTS":
                        transmitArgs.NumRequests = int.Parse(value);
                        break;
                    case "/NUMITERATIONS":
                        transmitArgs.NumIterations = int.Parse(value);
                        break;
                    case "/FRAMEDELAY":
                        transmitArgs.FrameDelay = TimeSpan.FromMilliseconds(int.Parse(value));
                        break;
                    default:
                        throw new InvalidOperationException("Invalid argument.");
                }
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
            Console.Out.WriteLine("The following optional flags are available:");
            Console.Out.WriteLine("/requestSize bytes - the request size.");
            Console.Out.WriteLine("/responseSize bytes - the response size.");
            Console.Out.WriteLine("/numRequests bytes - the number of requests.");
            Console.Out.WriteLine("/numIterations cycles - the number of cycles.");
            Console.Out.WriteLine("/frameDelay ms - the frame delay in ms.");
        }
    }
}
