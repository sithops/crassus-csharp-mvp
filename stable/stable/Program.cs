using CrassusClasses;
using CrassusProtocols;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Crassus
{
    public class Program
    {
        // Number of workers to use
        const int WorkerCount = 4;
        const int BufferSize = 4096;

        static CrassusState crassus = new CrassusState();

        // A static random generator object
        static Random random = new Random();

        // Optimizations for queue size lookups
        static int[] queueSizes;

        // Where we place our global objects (having no faster or better way to do it)
        static List<Worker> Workers = new List<Worker>();
        static Dictionary<string, WebSocketContainer> globalWebSockets =
            new Dictionary<string, WebSocketContainer>();

        // An object to check what versions of things we have availible
        static readonly PacketVersion PacketStructures = new PacketVersion();

        [Obsolete]
        public static void Main(string[] args)
        {
            int WorkerID = 0;

            queueSizes = new int[WorkerCount];

            // Create the workers but do not start them yet
            for (
                int Iterator = 0;
                Iterator < WorkerCount;
                Iterator++
            )
            {
                queueSizes[Iterator] = 0;

                Worker Worker = new Worker();
                Worker.Thread = new Thread(new ParameterizedThreadStart(Consumer));
                Worker.Queue = new BlockingCollection<(string, string)>();
                Worker.ID = WorkerID++;
                Workers.Add(Worker);
            }

            // Start the websocket endpoint
            WebSocketServer websocketServer = new WebSocketServer("ws://0.0.0.0:8080");
            websocketServer.AddWebSocketService("/", () => new ChannelAction("/"));
            websocketServer.Start();

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
                Console.WriteLine("OnOpen {0}", ID);

                // Add the websocket to the list
                globalWebSockets.Add(
                    ID,
                    new WebSocketContainer(
                        Context.WebSocket,
                        Guid.NewGuid()
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

            // Register us some queues and whatnot
            Worker Workload = new Worker();

            // A little debug
            Console.WriteLine("Worker: {0} started", workerID);

            // The initial negotiation
            // Wait for some work to do
            (
                string jsonPacket,
                string websocketID
            ) = Workers[workerID].Queue.Take();

            // Get a reference to our WebSocket
            WebSocketContainer webSocket = globalWebSockets[websocketID];

            JArray dataBlockMaster;
            IList<JToken> dataBlockChildren = new JArray();

            // Decrement the queue for this websocket
            queueSizes[workerID]--;

            try
            {
                // Parse the inbound packet
                dataBlockMaster = JArray.Parse(jsonPacket);
                // Convert into tokens
                dataBlockChildren = dataBlockMaster.Children().ToList();
            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception raised decoding json: {0}", exception.Message);
                webSocket.socket.Close();
                globalWebSockets.Remove(websocketID);
            }

            List<uint> pluginVersions = new List<uint>();

            foreach (JToken clientPluginVersion in dataBlockChildren)
            {
                pluginVersions.Add(clientPluginVersion.ToObject<uint>());
            }

            // Do some logic to figure out what we want to use ... accept it
            webSocket.protocolVersionSupport = PacketStructures.Negotiate(pluginVersions, true);
            webSocket.negotiatedVersion = true;

            // Send a response with our supported versions, highest first 
            // Send out via the websocket
            webSocket.socket.Send(
                JsonConvert.SerializeObject(
                    webSocket.protocolVersionSupport
                )
            );

            // If this was a blank array disconnect now
            if (webSocket.protocolVersionSupport.Length == 0)
            {
                webSocket.socket.Close();
                globalWebSockets.Remove(websocketID);
            }

            // If we got here we have some virgin we agree on
            webSocket.protocolVersionLock = webSocket.protocolVersionSupport[0];

            Protocol0 castPacket = new Protocol0();

            // Send a welcome header 
            Protocol[] greetingPacket = castPacket.Welcome(
                webSocket.InternalGuid,
                crassus.GetGuid()
            );

            string packet = JsonConvert.SerializeObject(greetingPacket);

            Console.WriteLine("Sending: {0}", packet);
            webSocket.socket.Send(packet);

            // We have a fully associated plugin lets deal with it
            while (true) {
                // Wait for some work to do
                (
                    jsonPacket,
                    websocketID
                ) = Workers[workerID].Queue.Take();

                // Decrement the queue for this websocket
                queueSizes[workerID]--;

                // Depending on version decrypt the packets correctly
                switch (webSocket.protocolVersionLock)
                {
                    case 0:
                        try
                        {
                            // Parse the inbound packet
                            dataBlockMaster = JArray.Parse(jsonPacket);
                            // Convert into tokens
                            dataBlockChildren = dataBlockMaster.Children().ToList();

                            rxheader = dataBlockChildren[0].ToObject<Protocol0Header>();
                            rxbody = dataBlockChildren[1].ToObject<Protocol0Body>();
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine("Exception raised: {0}", exception.Message);
                        }

                        break;
                    default:
                        Console.WriteLine(
                            "No idea what to do with version: '{0}', " +
                            "this client should not have sent this"
                        );
                        webSocket.socket.Close();
                        globalWebSockets.Remove(websocketID);
                        continue;
                }

                // Processing time!
                // If the destination is 00000-0000-000-00 etc then its for crassus its self
                Console.WriteLine("Packet receieved!");
            }
        }
        /*
        static string SomeCommand()
        {
            return new string(@"");
        }
        */
    }
}