﻿using System;
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
        static Dictionary<string, WebSocketContainer> id2WebSocket = new Dictionary<string, WebSocketContainer>();

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
                Worker.Queue = new BlockingCollection<(string,string)>();
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

        public class ChannelAction : WebSocketBehavior
        {

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
                id2WebSocket.Add(
                    ID, 
                    new WebSocketContainer(
                        Context.WebSocket,
                        Guid.Parse(internalGuidAsString)
                    )
                );
            }

            protected override void OnMessage(MessageEventArgs Packet)
            {
                
                // WARNING IT MIGHT BE FASTER TO JUST START FROM A GUESS POINT!
                int lowQueue = int.MaxValue;

                for (int workerID = 0; workerID < WorkerCount; workerID++)
                {
                    if (Workers[workerID].Queue.Count < lowQueue)
                    {
                        lowQueue = workerID;
                    }
                }

                Workers[lowQueue].Queue.Add((Packet.Data, ID));
            }

        }

        static void Consumer(Object MyID)
        {
            // Our ID
            int workerID = (int)MyID;

            // Pre create s apce for the header and body and internal guid
            Protocol rxheader = new Protocol();
            Protocol rxbody = new Protocol();

            // What packet to send out
            Protocol txheader = new Protocol();
            Protocol txbody = new Protocol();

            // Reusable internalGuid (Detect internal packets)
            Guid internalGuid = Guid.Parse(internalGuidAsString);

            // Register us some queues and whatnot
            Worker Workload = new Worker();

            // A little debug
            Console.WriteLine("Worker: {0} started", workerID);

            // The main work load
            while (true)
            {
                // Wait for some work to do
                (
                    string jsonPacket,
                    string websocketID
                ) = Workers[workerID].Queue.Take();

                QueueSizes[workerID]--;

                // Parse the inbound packet
                JArray dataBlockMaster = JArray.Parse(jsonPacket);

                // Convert into tokens
                IList<JToken> dataBlockChildren = dataBlockMaster.Children().ToList();

                // Get a reference to our WebSocket
                WebSocketContainer websocket = id2WebSocket[websocketID];

                // Le Logic time
                if (!websocket.negotiatedVersion)
                {
                    List<uint> pluginVersions = new List<uint>();

                    foreach (JToken clientPluginVersion in dataBlockChildren)
                    {
                        pluginVersions.Add(clientPluginVersion.ToObject<uint>());
                    }

                    // Do some logic to figure out what we want to use ... accept it
                    websocket.protocolVersionSupport    = PacketStructures.Negotiate(pluginVersions, true);
                    websocket.negotiatedVersion         = true;

                    // Send a response with our supported versions, highest first 
                    // Send out via the websocket
                    websocket.socket.Send(
                        JsonConvert.SerializeObject(
                            websocket.protocolVersionSupport
                        )
                    );
                }
                else
                {
                    // Extract the version token
                    rxheader = dataBlockChildren[0].ToObject<Protocol0>();
                    if (((Protocol0)rxheader).uuid.Equals(internalGuid))
                    {
                        // Someone tried to send a fake GUID or was to lazy to generate one, create one for them.
                        ((Protocol0)rxheader).uuid = Guid.NewGuid();
                    }
                    rxbody = dataBlockChildren[1].ToObject<Protocol1>();
                }             
            }

        }
    }
}