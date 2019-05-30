using System;
using System.Collections.Generic;
using WebSocketSharp;
using WebSocketSharp.Server;
using Newtonsoft.Json;

namespace Crassus
{
    public class Program
    {
        // Horrible globals that are going to cause god knows how many memcpy's
        static Dictionary<string, channel> Channels = new Dictionary<string, channel>();
        static Dictionary<string, Dictionary<string,bool>> Subscriptions = new Dictionary<string, Dictionary<string, bool>>();
        static Dictionary<string, WebSocket> Sockets = new Dictionary<string, WebSocket>();

        // Horrible way of making out switch work, thank god for POCs
        const string SUBSCRIBE = @"SUBSCRIBE";
        const string UNSUBSCRIBE = @"UNSUBSCRIBE";
        const string DATA = @"DATA";

        [Obsolete]
        public static void Main(string[] args)
        {
            /*
             * Add in what channels we want by default
             */

            Channels.Add(@"CHAN1", new channel(@"CHAN1"));
            Channels.Add(@"CHAN2", new channel(@"CHAN2"));
            Channels.Add(@"CHAN3", new channel(@"CHAN3"));

            /*
             * Start a websocket server on port 8080
             */

            var WebsocketServer = new WebSocketServer("ws://127.0.0.1:8080");
            WebsocketServer.AddWebSocketService("/", () => new ChannelAction("/"));
            WebsocketServer.Start();
            
            /*
             * Do a blank ReadKey to stop the server exiting
             */

            Console.ReadKey(true);
            WebsocketServer.Stop();
        }

        public class ChannelAction : WebSocketBehavior
        {
            private string _name = string.Empty;
            private string _prefix = string.Empty;
            private string Channel = string.Empty;

            /* 
             * No idea?
             */

            public ChannelAction()
                : this(null)
            {
            }

            /* 
             * Do not think we need this any more?
             */

            public ChannelAction(string prefix)
            {
                _prefix = !prefix.IsNullOrEmpty() ? prefix : "/";
            }

            /* 
             * When a connection is closed remove the memory of what channels its in
             */

            protected override void OnClose(CloseEventArgs Packet)
            {
                Console.WriteLine("WebSocket closed: {0}", ID);
                Subscriptions.Remove(ID);
            }

            /* 
             * When a connection is open make sure to register it a subscription dictionary(think db)
             */

            protected override void OnOpen()
            {
                Console.WriteLine("WebSocket open: {0}", ID);
                Subscriptions.Add(ID, new Dictionary<string, bool>());
            }

            protected override void OnMessage(MessageEventArgs Packet)
            {
                packet DataPacket = new packet();

                try
                {
                    Console.WriteLine("Attempting to deserialise: '{0}'", Packet.Data);
                    DataPacket = JsonConvert.DeserializeObject<packet>(Packet.Data);
                    if (DataPacket == null)
                    {
                        throw new Newtonsoft.Json.JsonSerializationException();
                    }
                    Console.WriteLine("Action type is: {0}",DataPacket.action);
                }
                catch (Newtonsoft.Json.JsonSerializationException Exception)
                {
                    Console.WriteLine("An exception was caused when deserializing '{0}', the exception raised was: '{1}'", Packet.Data, Exception.Message);
                    return;
                }
                catch (Newtonsoft.Json.JsonReaderException Exception)
                {
                    Console.WriteLine("An exception was caused when deserializing '{0}', the exception raised was: '{1}'", Packet.Data, Exception.Message);
                    return;
                }

                bool SkipBroadcast = false;

                switch (DataPacket.action.ToUpper())
                {
                    case DATA:
                        Channel = DataPacket.args[0].ToUpper();

                        if (DataPacket.args.Length == 0)
                        {
                            packet DataStreamError = new packet();
                            DataStreamError.action = @"DATA";
                            DataStreamError.args = new string[] { @"FAIL", @"No DATA" };
                        }
                        break;
                    case SUBSCRIBE:
                        Channel = DataPacket.args[0].ToUpper();

                        packet SubscribeResponse = new packet();
                        SubscribeResponse.action = @"SUBSCRIBE";

                        if (!Sockets.ContainsKey(ID))
                        {
                            Sockets.Add(ID, Context.WebSocket);
                        }

                        if (!Channels.ContainsKey(Channel))
                        {
                            SubscribeResponse.args = new string[] { @"FAIL", Channel, @"No such channel" };
                            Send(JsonConvert.SerializeObject(SubscribeResponse));

                            Console.WriteLine("Cannot subscribe client to channel, channel does not exist: '{0}'", Channel);
                        }
                        else
                        {
                            if (Subscriptions[ID].ContainsKey(Channel))
                            {
                                SubscribeResponse.args = new string[] { @"FAIL", Channel, @"You are already subscribed" };
                                Send(JsonConvert.SerializeObject(SubscribeResponse));

                                Console.WriteLine("Cannot subscribe client to channel, they are already in it: '{0}'", Channel);
                            }
                            else
                            {
                                SubscribeResponse.args = new string[] { @"SUCCESS", Channel };
                                Send(JsonConvert.SerializeObject(SubscribeResponse));

                                Subscriptions[ID].Add(Channel, true);

                                Console.WriteLine("Client subscribed to channel: '{0}'", Channel);
                            }
                        }
                        SkipBroadcast = true;
                        break;
                    case UNSUBSCRIBE:
                        Channel = DataPacket.args[0].ToUpper();

                        packet UnSubscribeResponse = new packet();
                        UnSubscribeResponse.action = @"UNSUBSCRIBE";

                        if (Subscriptions[ID].ContainsKey(Channel))
                        {
                            UnSubscribeResponse.args = new string[] { @"SUCCESS", Channel };
                            Send(JsonConvert.SerializeObject(UnSubscribeResponse));

                            Subscriptions[ID].Remove(Channel);

                            Console.WriteLine("Client unsubscribed from channel: '{0}'", Channel);
                        }
                        else
                        {
                            UnSubscribeResponse.args = new string[] { @"FAIL", Channel, @"You are already subscribed" };
                            Send(JsonConvert.SerializeObject(UnSubscribeResponse));

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

                    /* 
                     * No idea of the efficiency of this likely not high, but selective 
                     * send packets based on if someone is in a channel or not
                     */

                    foreach (string SesssionID in Sessions.IDs) {
                        Console.WriteLine("Sender session({0}), target session({1})",ID,SesssionID);
                        if (ID.Equals(SesssionID))
                        {
                            continue;
                        }
                        if (Subscriptions[SesssionID].ContainsKey(Channel))
                        {
                            Console.WriteLine("Send datapacket to {0}", SesssionID);
                            Sockets[SesssionID].Send(Packet.Data);
                        }
                    }
                }
            }
        }

        /*
         * What a packet looks like (so JSON can cast)
         */

        public class packet
        {
            public string action { get; set; }
            public string[] args { get; set; }
        }

        /* 
         * Define how channels are stored, this is really damn small but left like this so can be easily extended
         */

        public class channel
        {
            public channel(string PassedName)
            {
                name = PassedName;
            }

            public string name { get; internal set; }
        }

        /* Nyan cat for no real reason :)
         */

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
