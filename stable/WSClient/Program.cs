using System;
using WebSocketSharp;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using CrassusProtocols;
using CrassusClasses;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WSClient
{
    class Program
    {
        //static readonly PacketVersion PacketVersions = new PacketVersion();
        static List<uint> protocolVersion = new List<uint>();

        static void Main(string[] args)
        {
            WebSocket websocket = new WebSocket("ws://127.0.0.1:8080");
            int packetSeq = 0;

            Protocol0Header rxheader;
            Protocol0Body rxbody;

            Guid ourUUID = Guid.Empty;
            Guid crassusUUID = Guid.Empty;

            Random random = new Random();

            // Shortcut functions
            Protocol0 fastPacket = new Protocol0();

            websocket.OnMessage += (sender, data) =>
            {
                // Parse the inbound packet
                JArray dataBlockMaster = JArray.Parse(data.Data);

                // Convert into tokens
                IList<JToken> dataBlockChildren = dataBlockMaster.Children().ToList();

                // PacketSeq
                packetSeq++;

                //Console.WriteLine("Json RX: {0}", data.Data);

                if (packetSeq == 1)
                {
                    foreach (JToken clientPluginVersion in dataBlockChildren)
                    {
                        protocolVersion.Add(clientPluginVersion.ToObject<uint>());
                        Console.WriteLine("Server supports: {0}", clientPluginVersion);
                    }

                    if (protocolVersion.Count == 0)
                    {
                        Console.WriteLine("We do not support any mutual version the server does!");
                        websocket.Close();
                    }
                    else
                    {
                        Console.WriteLine("Using protocol: {0}", protocolVersion[0]);
                    }
                }
                else if (packetSeq == 2)
                {
                    rxheader = dataBlockChildren[0].ToObject<Protocol0Header>();
                    rxbody = dataBlockChildren[1].ToObject<Protocol0Body>();

                    Guid[] decodedJson = JsonConvert.DeserializeObject<Guid[]>(rxbody.data);
                    crassusUUID = decodedJson[0];
                    ourUUID = decodedJson[1];

                    Console.WriteLine(
                        "Our UUID is: {0},\nThe crassus controller is on: {1}",
                        ourUUID,
                        crassusUUID
                    );

                    // Lets send a subscribe!
                    string channelQuery = fastPacket.CrassusCommand(
                        crassusUUID,
                        ourUUID,
                        new string[] { "SUBSCRIPTION","LIST" }
                    );

                    websocket.Send(channelQuery);
                }
                else if (packetSeq == 3)
                {
                    rxheader = dataBlockChildren[0].ToObject<Protocol0Header>();
                    rxbody = dataBlockChildren[1].ToObject<Protocol0Body>();

                    string[] channels = JsonConvert.DeserializeObject<string[]>(rxbody.data);

                    foreach (string channel in channels)
                    {
                        Console.WriteLine("Server has a channel: {0}", channel);

                        if (!channel.Equals("ECHO")) { continue; }

                        websocket.Send(
                            fastPacket.CrassusCommand(
                                crassusUUID,
                                ourUUID,
                                new string[] { "SUBSCRIPTION", "ADD", channel }
                            )
                        );
                    }

                }
                else
                {
                    rxheader = dataBlockChildren[0].ToObject<Protocol0Header>();
                    rxbody = dataBlockChildren[1].ToObject<Protocol0Body>();
                }
                // Elsewhere this is a normal packet!

            };

            websocket.OnOpen += (sender, data) =>
            {
            };

            websocket.OnClose += (sender, data) =>
            {
            };

            websocket.Connect();

            {
                websocket.Send(JsonConvert.SerializeObject(PacketVersions.ToArray()));
            }
            
            Console.ReadKey();
        }
    }
}
