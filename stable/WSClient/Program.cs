using System;
using WebSocketSharp;
using Newtonsoft.Json;

using CrassusProtocols;
using CrassusClasses;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace WSClient
{
    class Program
    {
        static readonly PacketVersion PacketVersions = new PacketVersion();
        static bool FirstResp = true;
        static uint[] protocolVersion;

        static void Main(string[] args)
        {   
            WebSocket websocket = new WebSocket("ws://127.0.0.1:8080");

            websocket.OnMessage += (sender, data) =>
            {
                // Parse the inbound packet
                JArray dataBlockMaster = JArray.Parse(data.Data);

                // Convert into tokens
                IList<JToken> dataBlockChildren = dataBlockMaster.Children().ToList();

                if (FirstResp)
                {
                    FirstResp = false;
                    List<uint> protocolVersion = new List<uint>();
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
                        Console.WriteLine("Using: {0} by default", protocolVersion[0]);
                    }
                }
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

            /*
            for (int i = 0; i < 10; i++)
            {
                // Send packets to ourself for testing
                Protocol[] JSONPacketData = new Protocol[2];

                // Initilize the store for the version number
                JSONPacketData[0] = new Protocol0(1, Guid.NewGuid());

                if (((Protocol0)JSONPacketData[0]).version == 1)
                {
                    JSONPacketData[1] = new Protocol1();
                    // Bind the data to the appropriate parts of the packet
                    ((Protocol1)JSONPacketData[1]).crassus = new uint[] { 0,1,100 };

                    websocket.Send(JsonConvert.SerializeObject(JSONPacketData));
                }
            }
            */
        }
    }
}
