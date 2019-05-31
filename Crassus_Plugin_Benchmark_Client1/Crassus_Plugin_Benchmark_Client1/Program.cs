using WebSocketSharp;
using WebSocketSharp.Server;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Crassus_Plugin_Benchmark_Client1
{
    class Program
    {
        static public Random RandomGenerator = new Random();
        static public Dictionary<string, string> JSONPackets = new Dictionary<string, string>();

        static void Main(string[] args)
        {
            Dictionary<int, WebSocket> Sockets = new Dictionary<int, WebSocket>();

            packet JSONPacket = new packet();

            JSONPacket.action = @"SUBSCRIBE";
            JSONPacket.args = new string[] { "CHAN1" };
            JSONPackets.Add("SUBSCRIBE1", JsonConvert.SerializeObject(JSONPacket));

            JSONPacket.action = @"SUBSCRIBE";
            JSONPacket.args = new string[] { "CHAN2" };
            JSONPackets.Add("SUBSCRIBE2", JsonConvert.SerializeObject(JSONPacket));

            JSONPacket.action = @"SUBSCRIBE";
            JSONPacket.args = new string[] { "CHAN3" };
            JSONPackets.Add("SUBSCRIBE3", JsonConvert.SerializeObject(JSONPacket));

            JSONPacket.action = @"UNSUBSCRIBE";

            JSONPackets.Add("UNSUBSCRIBE", JsonConvert.SerializeObject(JSONPacket));

            JSONPacket.action = @"ECHO";
            JSONPacket.args = new string[] { "ECHO DATA" };

            JSONPackets.Add("ECHO", JsonConvert.SerializeObject(JSONPacket));

            for (int SocketNum = 0; SocketNum < 4; SocketNum++)
            {
                Sockets.Add(SocketNum,new WebSocket("ws://rice.daemon.space:8080"));
                WebSocket websocket = Sockets[SocketNum];

                websocket.OnMessage += (sender, data) =>
                {
                    packet DataPacket = JsonConvert.DeserializeObject<packet>(data.Data);

                    if (DataPacket.action.Equals("SUBSCRIBE"))
                    {
                        packet JSONPacketData = new packet();
                        JSONPacketData.action = @"DATA";
                        JSONPacketData.args = new string[] { DataPacket.args[1], "DATA Block" };

                        websocket.Send(JsonConvert.SerializeObject(JSONPacketData));
                    }
                    else if (boolean_random()) { 
                        {
                            JSONPacket.action = @"DATA";
                            JSONPacket.args = new string[] { DataPacket.args[0], "SOMEDATA" };

                            try
                            {
                                websocket.Send(JsonConvert.SerializeObject(JSONPacket));
                            }
                            catch
                            {

                            }
                        }
                    }
                };

                websocket.OnOpen += (sender, data) =>
                {
                    for (int ChannelNumber = 1; ChannelNumber < 4; ChannelNumber++)
                    {
                        if (boolean_random())
                        {
                            websocket.Send(JSONPackets["SUBSCRIBE" + ChannelNumber]);
                        }
                    }
                };

                websocket.OnClose += (sender, data) =>
                {
                };

                websocket.Connect();
            }

            Console.ReadKey(true);
        }

        internal class packet
        {
            public string action { get; set; }
            public string[] args { get; set; }
        }

        static bool boolean_random()
        {
            if (RandomGenerator.Next(100) > 25)
            {
                return true;
            }
            return false;
        }
    }
}
