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
using Newtonsoft.Json;

namespace Crassus
{
    public class Program
    {
        // Number of workers to use
        const int       WorkerCount             = 4;
        const int       BufferSize              = 4096;
        const string    internalGuidAsString    = @"00000000-0000-0000-0000-000000000000";

        // A static random generator object
        static Random random = new Random();

        // Optimizations for queue size lookups
        static int[] QueueSizes;

        // Where we place our global objects (having no faster or better way to do it)
        static List<Worker> Workers = new List<Worker>();
        static Dictionary<string, WebSocket> id2WebSocket = new Dictionary<string, WebSocket>();

        // An object to check what versions of things we have availible
        static readonly PacketVersion PacketStructures = new PacketVersion();

        [Obsolete]
        public static void Main(string[] args)
        {
            int WorkerID = 0;

            QueueSizes = new int[WorkerCount];

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
            // Our ID
            int ID = (int)MyID;

            // Pre create s apce for the header and body and internal guid
            Protocol Header     = new Protocol();
            Protocol Body       = new Protocol();
            Guid internalGuid   = Guid.Parse(internalGuidAsString);

            // Register us some queues and whatnot
            Worker Workload     = new Worker();

            // A little debug
            Console.WriteLine("Worker: {0} started",ID);

            // The main work load
            while (true) {
                // Wait for some work to do
                Protocol[] NewPacket = Workers[ID].Queue.Take();

                // Atomic so cannot be blocked
                QueueSizes[ID]--;

                // Maybe this is a new client, check for our internal UUID
                if (((Protocol0)NewPacket[0]).uuid.Equals(internalGuid))
                {
                    // Ah it is 
                    Console.WriteLine("Internal packet detected! (Using protocolx)");
                    uint[] Versions = ((ProtocolX)NewPacket[1]).versions;
                    foreach (uint PossibleVersion in Versions)
                    {
                        Console.WriteLine("Version detected: {0}", PossibleVersion);
                    }
                }
                else
                {
                    // Just dump the UUID for now
                    Console.WriteLine(
                        "Processed packet from: {0}",
                        ((Protocol0)NewPacket[0]).uuid
                    );
                }
            }
        }

        public class ChannelAction : WebSocketBehavior
        {
            private ConcurrentBag<Protocol[]> SendQueue;

            // Initial connection detect
            private bool negotiatedVersion = false;
            private uint[] protocolVersionSupport;

            // Just to speed up processing slightly
            private Guid internalGuid;
            private Protocol header = new Protocol();
            private Protocol Body = new Protocol();

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

                // Generate cache items
                internalGuid = Guid.Parse(internalGuidAsString);

                // Add the websocket to the list
                id2WebSocket.Add(ID, Context.WebSocket);
            }

            protected override void OnMessage(MessageEventArgs Packet)
            {
                // Parse the inbound packet
                JArray dataBlockMaster = JArray.Parse(Packet.Data);
                
                // Convert into tokens
                IList<JToken> dataBlockChildren = dataBlockMaster.Children().ToList();

                // Le Logic time
                if (!negotiatedVersion)
                {
                    List<uint> pluginVersions = new List<uint>();

                    foreach (JToken clientPluginVersion in dataBlockChildren)
                    {
                        pluginVersions.Add(clientPluginVersion.ToObject<uint>());
                    }

                    // Do some logic to figure out what we want to use ... accept it
                    protocolVersionSupport  = PacketStructures.Negotiate(pluginVersions,true);
                    negotiatedVersion       = true;

                    // Generate a header
                    header  = new Protocol0(protocolVersionSupport[0],internalGuid);
                    Body    = new ProtocolX(protocolVersionSupport);
                }
                else
                {
                    // Extract the version token
                    header = dataBlockChildren[0].ToObject<Protocol0>();
                    if (((Protocol0)header).uuid.Equals(internalGuid))
                    {
                        // Someone tried to send a fake GUID or was to lazy to generate one, create one for them.
                        ((Protocol0)header).uuid = Guid.NewGuid();
                    }
                    Body = dataBlockChildren[1].ToObject<Protocol1>();
                }

                // WARNING IT MIGHT BE FASTER TO JUST START FROM A GUESS POINT!
                int lowQueue = int.MaxValue;

                for (int workerID = 0; workerID < WorkerCount; workerID++)
                {
                    if (Workers[workerID].Queue.Count < lowQueue)
                    {
                        lowQueue = workerID;
                    }
                }

                Workers[lowQueue].Queue.Add(new Protocol[] { header, Body });
            }

        }
    }
}