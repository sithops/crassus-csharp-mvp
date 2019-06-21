using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Linq;

using WebSocketSharp;
using WebSocketSharp.Server;

using Newtonsoft.Json.Linq;

using CrassusProtocols;
using CrassusClasses;


namespace Crassus
{
    public class Program
    {
        // Number of workers to use
        const int WorkerCount = 4;
        const int BufferSize = 4096;

        // A static random generator object
        static Random random = new Random();

        // Optimizations for queue size lookups
        static uint[] QueueSizes;

        // Where we place our global objects (having no faster or better way to do it)
        static List<Worker> Workers = new List<Worker>();
        static Dictionary<string, WebSocket> ID2WebSocket = new Dictionary<string, WebSocket>();
        //static Dictionary<string,List<string>> Subscriptions = new Dictionary<string,List<string>>();

        //static ConcurrentDictionary<string, int> Channels = new ConcurrentDictionary<string, int>();
        //static ConcurrentDictionary<Plugin,int> Plugins = new ConcurrentDictionary<Plugin,int>();

        // It is all here, all you need is a way to reference other sockets in a broadcast
        // And also keep some form of fast lookup between connections

        [Obsolete]
        public static void Main(string[] args)
        {
            int WorkerID = 0;

            QueueSizes = new uint[WorkerCount];

            DateTime StartTime = DateTime.Now;

            // Create the workers but do not start them yet
            for (
                int Iterator = 0;
                Iterator < WorkerCount;
                Iterator++
            )
            {
                QueueSizes[Iterator] = 0;

                Worker Worker = new Worker();
                Worker.Thread = new Thread(new ParameterizedThreadStart(Consumer));
                Worker.Queue = new BlockingCollection<Protocol[]>();
                Worker.ID = WorkerID++;
                Workers.Add(Worker);
            }

            // Start the websocket endpoint
            WebSocketServer WebsocketServer = new WebSocketServer("ws://0.0.0.0:8080");
            WebsocketServer.AddWebSocketService("/", () => new ChannelAction("/"));
            WebsocketServer.Start();

            // Start the workers off
            for (
                int Iterator = 0;
                Iterator < WorkerCount;
                Iterator++
            )
            {
                Workers[Iterator].Thread.Start(--WorkerID);
            }
            
            // In the main thread we will do some shit?
            Console.ReadKey();
        }

        static void Consumer(Object MyID)
        {
            int ID = (int)MyID;

            Worker Workload = new Worker();
            Console.WriteLine("Thread started");

            while (true) {
                Protocol[] NewPacket = Workers[ID].Queue.Take();
                // Atomic so cannot be blocked
                QueueSizes[ID]--;
                // Just dump the UUID for now
                Console.WriteLine(
                    "Processed packet from: {0}",
                    ((Protocol0)NewPacket[0]).uuid
                );
            }
        }

        public class ChannelAction : WebSocketBehavior
        {
            private ConcurrentBag<Protocol[]> SendQueue;
            private bool NegotiatedVersion = false;
            private uint ProtocolVersion = 0;

            public ChannelAction() : this(null)
            {
            }

            public ChannelAction(string _)
            {
            }

            protected override void OnClose(CloseEventArgs Packet)
            {
                Console.WriteLine("OnClose");

            }

            protected override void OnOpen()
            {
                // Create an entry in the static/Plugins defining what we are
                // As for channels, each plugin can have .... 
                Console.WriteLine("OnOpen {0}",ID);

                // Add the websocket to the list
                ID2WebSocket.Add(ID, Context.WebSocket);
            }

            protected override void OnMessage(MessageEventArgs Packet)
            {
                // Parse the inbound packet
                JArray DataBlockMaster = JArray.Parse(Packet.Data);

                // Convert into tokens
                IList<JToken> DataBlockChildren = DataBlockMaster.Children().ToList();

                if (!NegotiatedVersion)
                {
                    uint[] SupportedVersions = DataBlockChildren[0].ToObject<uint[]>();
                    // Do some logic to figure out what we want to use ... accept it
                    ProtocolVersion = SupportedVersions[(SupportedVersions.Length - 1)];
                    NegotiatedVersion = true;
                }

                // Extract the version token
                Protocol0 Header = DataBlockChildren[0].ToObject<Protocol0>();

                // Extract the data token
                Protocol Body = new Protocol();

                // WARNING IT MIGHT BE FASTER TO JUST START FROM A GUESS POINT!
                int LowQueue = int.MaxValue;

                for (int WorkerID = 0;WorkerID < WorkerCount;WorkerID++)
                {
                    if (Workers[WorkerID].Queue.Count < LowQueue)
                    {
                        LowQueue = WorkerID;
                    }
                }

                if (Header.version == 1)
                {
                    Body = DataBlockChildren[1].ToObject<Protocol1>();
                    Workers[LowQueue].Queue.Add(new Protocol[] { Header, Body });
                }
                else
                {
                    Console.WriteLine("Dropped packet due to unknown version");
                }
            }
        }
    }
}