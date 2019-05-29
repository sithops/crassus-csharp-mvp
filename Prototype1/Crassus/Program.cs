using System;
using System.Collections.Generic;
using WebSocketSharp;
using WebSocketSharp.Server;
using Newtonsoft.Json;

namespace Crassus
{
    public class Program
    {
        static Dictionary<string, channel> Channels = new Dictionary<string, channel>();

        // Horrible way of making out switch work, thank god for POCs
        const string SUBSCRIBE = @"SUBSCRIBE";
        const string UNSUBSCRIBE = @"UNSUBSCRIBE";
        const string STREAMDATA = @"STREAMDATA";

        [Obsolete]
        public static void Main(string[] args)
        {
            Channels.Add(@"CHAN1", new channel(@"CHAN1"));
            Channels.Add(@"CHAN2", new channel(@"CHAN2"));
            Channels.Add(@"CHAN3", new channel(@"CHAN3"));

            var WebsocketServer = new WebSocketServer("ws://127.0.0.1:8080");
            WebsocketServer.AddWebSocketService("/", () => new ChannelAction("/"));
            WebsocketServer.Start();

            Console.ReadKey(true);
            WebsocketServer.Stop();
        }

        public class ChannelAction : WebSocketBehavior
        {
            private string _name = string.Empty;
            private string _prefix = string.Empty;
            private string Channel = string.Empty;

            private Dictionary<string, bool> SubscribedChannels = new Dictionary<string, bool>();

            public ChannelAction()
                : this(null)
            {
            }

            public ChannelAction(string prefix)
            {
                _prefix = !prefix.IsNullOrEmpty() ? prefix : "/";
            }

            protected override void OnClose(CloseEventArgs Packet)
            {
                Sessions.Broadcast(string.Format("{0} got logged off...", _name));
            }

            protected override void OnMessage(MessageEventArgs Packet)
            {
                packet DataPacket = new packet();

                try
                {
                    Console.WriteLine("Attempting to deserialise: '{0}'", Packet.Data);
                    DataPacket = JsonConvert.DeserializeObject<packet>(Packet.Data);
                    Console.WriteLine("Action type is: {0}",DataPacket.action);
                }
                catch (Newtonsoft.Json.JsonSerializationException Exception)
                {
                    Console.WriteLine("An exception was caused when deserializing '{0}', the exception raised was: '{1}'", Packet.Data, Exception.Message);
                    return;
                }

                bool SkipBroadcast = false;

                switch (DataPacket.action.ToUpper())
                {
                    case STREAMDATA:

                        break;
                    case SUBSCRIBE:
                        Channel = DataPacket.args[0].ToUpper();

                        packet DataResponse = new packet();
                        DataResponse.action = @"SUBSCRIBE";

                        if (SubscribedChannels.ContainsKey(Channel))
                        {
                            // Return an error? Or just ignore it

                            DataResponse.args = new string[] { @"FAIL", Channel, @"You are already subscribed" };
                            Send(JsonConvert.SerializeObject(DataResponse));

                            Console.WriteLine("Cannot subscribe client to channel, they are already in it: '{0}'", Channel);
                        }
                        else
                        {
                            DataResponse.args = new string[] { @"SUCCESS", Channel };
                            Send(JsonConvert.SerializeObject(DataResponse));

                            SubscribedChannels.Add(Channel, true);

                            Console.WriteLine("Client subscribed to channel: '{0}'", Channel);
                        }
                        SkipBroadcast = true;
                        break;
                    case UNSUBSCRIBE:
                        Channel = DataPacket.args[0].ToUpper();
                        if (SubscribedChannels.ContainsKey(Channel))
                        {
                            SubscribedChannels.Remove(Channel);
                            Console.WriteLine("Client unsubscribed from channel: '{0}'", Channel);
                        }
                        else
                        {
                            // Return an error? Or just ignore it
                            Console.WriteLine("Cannot unsubscribe client from channel, they are not in it: '{0}'", Channel);
                        }
                        SkipBroadcast = true;
                        break;
                    default:
                        Console.WriteLine("Got a request for action: {0}, no idea what it is",DataPacket.action.ToUpper());
                        SkipBroadcast = true;
                        break;
                }

                if (!SkipBroadcast) {
                    
                    //Sessions.Broadcast(string.Format(Packet.Data));
                }
            }

            //protected override void OnOpen()
            //{
            //}
        }

        public class packet
        {
            public string action { get; set; }
            public string[] args { get; set; }
        }

        public class channel
        {
            public channel(string PassedName)
            {
                name = PassedName;
            }

            public string name { get; internal set; }
        }

        public static string nyan()
        {
            return @"________▄▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▄______
_______█░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░█_____
_______█░▒▒▒▒▒▒▒▒▒▒▄▀▀▄▒▒▒░░█▄▀▀▄_
__▄▄___█░▒▒▒▒▒▒▒▒▒▒█▓▓▓▀▄▄▄▄▀▓▓▓█_ 
█▓▓█▄▄█░▒▒▒▒▒▒▒▒▒▄▀▓▓▓▓▓▓▓▓▓▓▓▓▀▄_ 
_▀▄▄▓▓█░▒▒▒▒▒▒▒▒▒█▓▓▓▄█▓▓▓▄▓▄█▓▓█_ 
_____▀▀█░▒▒▒▒▒▒▒▒▒█▓▒▒▓▄▓▓▄▓▓▄▓▒▒█ 
______▄█░░▒▒▒▒▒▒▒▒▒▀▄▓▓▀▀▀▀▀▀▀▓▄▀_ 
____▄▀▓▀█▄▄▄▄▄▄▄▄▄▄▄▄██████▀█▀▀___ 
____█▄▄▀_█▄▄▀_______█▄▄▀_▀▄▄█_____";
        }
    }
}
