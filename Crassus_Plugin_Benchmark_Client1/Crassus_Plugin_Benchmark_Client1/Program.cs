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
            Dictionary<string, WebSocket> Sockets = new Dictionary<string, WebSocket>();

            packet JSONPacket = new packet();

            JSONPacket.action = @"SUBSCRIBE1";
            JSONPacket.args = new string[] { "CHAN1" };
            JSONPackets.Add("SUBSCRIBE1", JsonConvert.SerializeObject(JSONPacket));

            JSONPacket.action = @"SUBSCRIBE2";
            JSONPacket.args = new string[] { "CHAN2" };
            JSONPackets.Add("SUBSCRIBE2", JsonConvert.SerializeObject(JSONPacket));

            JSONPacket.action = @"SUBSCRIBE3";
            JSONPacket.args = new string[] { "CHAN3" };
            JSONPackets.Add("SUBSCRIBE3", JsonConvert.SerializeObject(JSONPacket));

            JSONPacket.action = @"UNSUBSCRIBE";

            JSONPackets.Add("UNSUBSCRIBE", JsonConvert.SerializeObject(JSONPacket));

            JSONPacket.action = @"DATA";
            JSONPacket.args = new string[] { "DATA Block" };

            JSONPackets.Add("DATA", JsonConvert.SerializeObject(JSONPacket));

            for (int i = 0; i < 2; i++)
            {
                WebSocket websocket = new WebSocket("ws://rice.daemon.space:8080");
                websocket.OnMessage += ((sender, data) => new WSClient());
                //websocket.OnOpen += ((sender, data) => new WSClient());
                //websocket.OnClose += ((sender, data) => new WSClient());
                websocket.Connect();
                //websocket.Send(JSONPackets["SUBSCRIBE"]);
            }

            //websocket.Send(UnSubscribePacketAsText);

            Console.ReadKey(true);
        }

        internal class packet
        {
            public string action { get; set; }
            public string[] args { get; set; }
        }

        static bool boolean_random()
        {
            if (RandomGenerator.Next(100) > 50)
            {
                return true;
            }
            return false;
        }


        protected void OnOpen()
        {
            Console.WriteLine("x");
            for (int i = 1; i < 4; i++)
            {
                Console.WriteLine("ID {0}", i);
                if (boolean_random())
                {
                    string SPacket = "SUBSCRIBE" + i;
                    //Send(JSONPackets["SPacket"]);
                }
            }
        }
        public class WSClient : WebSocketBehavior
        {
            protected override void OnMessage(MessageEventArgs Packet)
            {
                Console.WriteLine("[{0}] Data: {1}", ID, Packet.Data);
            }

            

            protected override void OnClose(CloseEventArgs Packet)
            {

            }
            
        }
    }
}
