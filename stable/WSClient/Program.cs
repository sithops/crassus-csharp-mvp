using System;
using WebSocketSharp;
using Newtonsoft.Json;

using CrassusProtocols;
using CrassusClasses;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WSClient
{
    class Program
    {
        static readonly PacketVersion PacketVersions = new PacketVersion();
        static bool FirstResp = true;
        static List<uint> protocolVersion = new List<uint>();

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
                else
                {
                    Console.WriteLine("Json RX: {0}", data.Data);
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
