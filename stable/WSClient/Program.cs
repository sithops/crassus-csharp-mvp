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
                        Console.WriteLine("Using: {0} by default", protocolVersion[0]);
                        // Send the first broadcast message!
                        // Read in an image file
                        //string text = File.ReadAllText(@"c:\img.png");

                        Protocol header = new ProtocolHeader0(protocolVersion[0]);
                        Protocol body = new ProtocolBody0();

                        ((ProtocolBody0)body).crassus = Encoding.UTF8.GetBytes(@"Hello World");
                        ((ProtocolBody0)body).option = new Dictionary<string, string>
                        {
                            { "report","report_clients" }
                        };
                        ((ProtocolBody0)body).routing = new Dictionary<string, Guid>
                        {
                            { "destination",Guid.NewGuid() },
                            { "source",Guid.NewGuid()}
                        };

                        Console.WriteLine(
                            JsonConvert.SerializeObject(
                                new Protocol[] { header, body }
                            )
                        );

                        websocket.Send(
                            JsonConvert.SerializeObject(
                                new Protocol[] { header, body }
                            )
                        );
                    }
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
