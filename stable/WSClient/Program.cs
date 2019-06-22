using System;
using WebSocketSharp;
using Newtonsoft.Json;

using CrassusProtocols;
using CrassusClasses;

namespace WSClient
{
    class Program
    {
        static readonly PacketVersion PacketVersions = new PacketVersion();

        static void Main(string[] args)
        {   
            WebSocket websocket = new WebSocket("ws://127.0.0.1:8080");

            websocket.OnMessage += (sender, data) =>
            {
                
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
