using System;
using System.Collections.Generic;
using WebSocketSharp;
using WebSocketSharp.Server;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Threading;

namespace Crassus
{
    public class Program
    {
        // Horrible globals that are going to cause god knows how many memcpy's
        static Dictionary<string, channel> Channels = new Dictionary<string, channel>();
        static ConcurrentDictionary<string, Dictionary<string,bool>> Subscriptions = new ConcurrentDictionary<string, Dictionary<string, bool>>();
        static ConcurrentDictionary<string, WebSocket> Sockets = new ConcurrentDictionary<string, WebSocket>();

        // Horrible way of making our switch work, thank god for POCs
        const string SUBSCRIBE = @"SUBSCRIBE";
        const string UNSUBSCRIBE = @"UNSUBSCRIBE";
        const string DATA = @"DATA";
        const string ECHO = @"ECHO";

        // For metrics
        static uint TotalSent = 0;
        static uint TotalBroadcast = 0;
        static uint TotalDisplay = 1024;
        static DateTime StartTime = DateTime.Now;

        [Obsolete]
        public static void Main(string[] args)
        {
            /*
             * Add in what channels we want by default
             */

            Channels.Add(@"CHAN1", new channel(@"CHAN1"));
            Channels.Add(@"CHAN2", new channel(@"CHAN2"));
            Channels.Add(@"CHAN3", new channel(@"CHAN3"));
            Channels.Add(@"NYAN", new channel(@"NYAN",true));

            /*
             * Start a websocket server on port 8080
             */

            var WebsocketServer = new WebSocketServer("ws://0.0.0.0:8080");
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
                //Console.WriteLine("[{0}] Closed", ID);
                /*
                 * tidy up
                 */
                
                while (!Subscriptions.TryRemove(ID, out _)) { Thread.Sleep(1);  }
                while (!Sockets.TryRemove(ID, out _)) { Thread.Sleep(1); }
            }

            /* 
             * When a connection is open make sure to register it a subscription dictionary(think db)
             */

            protected override void OnOpen()
            {
                //Console.WriteLine("[{0}] Open", ID);
                while (
                    !Subscriptions.TryAdd(
                        ID, 
                        new Dictionary<string, bool>()
                    )
                )
                {
                    Thread.Sleep(1);
                }
            }

            protected override void OnMessage(MessageEventArgs Packet)
            {
                packet DataPacket = new packet();

                //Console.WriteLine("[{0}] JSON-Deserialize: '{1}'", ID, Packet.Data);

                if (TotalDisplay-- == 0)
                {
                    TotalDisplay = 1024;
                    double DiffInSeconds = (DateTime.Now - StartTime).TotalSeconds;
                    Console.WriteLine(
                        "In {0} seconds I have processed: {1} Sends and {2} Broadcasts",
                        DiffInSeconds,
                        TotalSent,
                        TotalBroadcast
                    );
                }

                try
                {
                    DataPacket = JsonConvert.DeserializeObject<packet>(Packet.Data);
                    if (
                        DataPacket == null 
                        || DataPacket.action.IsNullOrEmpty()
                        || DataPacket.args.Length == 0
                    )
                    {
                        throw new Newtonsoft.Json.JsonSerializationException();
                    }
                }
                catch (Newtonsoft.Json.JsonSerializationException Exception)
                {
                    Console.WriteLine("[{0}] An exception was caused when deserializing '{1}', the exception raised was: '{2}'", ID, Packet.Data, Exception.Message);
                    return;
                }
                catch (Newtonsoft.Json.JsonReaderException Exception)
                {
                    Console.WriteLine("[{0}] An exception was caused when deserializing '{1}', the exception raised was: '{2}'", ID, Packet.Data, Exception.Message);
                    return;
                }

                //Console.WriteLine("[{0}] Action: {1}", ID, DataPacket.action);

                bool SkipBroadcast = false;
                Channel = DataPacket.args[0].ToUpper();

                switch (DataPacket.action.ToUpper())
                {
                    case DATA:
                        /* 
                         * When anything on the channel with nyan_flag is set, send a nyan cat to everyone
                         * including the person who sent it
                         */



                        if (DataPacket.args.Length < 2 || !Channels.ContainsKey(Channel))
                        {
                            packet DataStreamError = new packet();
                            DataStreamError.action = @"DATA";
                            DataStreamError.args = new string[] { @"FAIL", @"No DATA" };

                            Send(JsonConvert.SerializeObject(DataStreamError));

                            SkipBroadcast = true;
                        }
                        else if (Channels[Channel].flag_nyan) {
                            packet NyanCat = new packet();
                            NyanCat.action = @"DATA";
                            NyanCat.args = new string[] { @"SUCCESS", nyan() };
                            Send(JsonConvert.SerializeObject(NyanCat));
                        }

                        break;
                    case ECHO:
                        packet EchoPacket = new packet();
                        EchoPacket.action = @"ECHO";
                        EchoPacket.args = new string[] { DataPacket.args[0] };

                        Send(JsonConvert.SerializeObject(EchoPacket));

                        SkipBroadcast = true;
                        break;
                    case SUBSCRIBE:
                        packet SubscribeResponse = new packet();
                        SubscribeResponse.action = @"SUBSCRIBE";

                        if (!Sockets.ContainsKey(ID))
                        {
                            while (!
                                Sockets.TryAdd(
                                    ID,
                                    Context.WebSocket
                                )
                            )
                            {
                                Thread.Sleep(1);
                            }
                        }

                        if (!Channels.ContainsKey(Channel))
                        {
                            SubscribeResponse.args = new string[] { @"FAIL", Channel, @"No such channel" };
                            Send(JsonConvert.SerializeObject(SubscribeResponse));

                            Console.WriteLine("[{0}] Cannot subscribe client to channel, channel does not exist: '{1}'", ID, Channel);
                        }
                        else if (Subscriptions[ID].ContainsKey(Channel))
                        {
                            SubscribeResponse.args = new string[] { @"FAIL", Channel, @"You are already subscribed" };
                            Send(JsonConvert.SerializeObject(SubscribeResponse));

                            Console.WriteLine("[{0}] Cannot subscribe client to channel, they are already in it: '{1}'", ID, Channel);
                        }
                        else
                        {
                            SubscribeResponse.args = new string[] { @"SUCCESS", Channel };
                            Send(JsonConvert.SerializeObject(SubscribeResponse));

                            Subscriptions[ID].Add(Channel, true);

                            //Console.WriteLine("[{0}] Client subscribed to channel: '{1}'", ID, Channel);
                        }

                        SkipBroadcast = true;
                        break;
                    case UNSUBSCRIBE:
                        packet UnSubscribeResponse = new packet();
                        UnSubscribeResponse.action = @"UNSUBSCRIBE";

                        if (Subscriptions[ID].ContainsKey(Channel))
                        {
                            UnSubscribeResponse.args = new string[] { @"SUCCESS", Channel };
                            Send(JsonConvert.SerializeObject(UnSubscribeResponse));

                            Subscriptions[ID].Remove(Channel);

                            //Console.WriteLine("[{0}] Client unsubscribed from channel: '{1}'", ID,  Channel);
                        }
                        else
                        {
                            UnSubscribeResponse.args = new string[] { @"FAIL", Channel, @"You are already subscribed" };
                            Send(JsonConvert.SerializeObject(UnSubscribeResponse));

                            Console.WriteLine("[{0}] Cannot unsubscribe client from channel, they are not in it: '{1}'", ID, Channel);
                        }
                        SkipBroadcast = true;
                        break;
                    default:
                        Console.WriteLine("[{0}] Got a request for action: {1}, no idea what it is", ID, DataPacket.action.ToUpper());
                        SkipBroadcast = true;
                        break;
                }

                if (!SkipBroadcast) {

                    /* 
                     * No idea of the efficiency of this likely not high, but selective 
                     * send packets based on if someone is in a channel or not
                     * 
                     * Really should dump a [WebSocket,DATA] into a FIFO queue and use a thread 
                     * for processing that queue, incase a WS goes missing
                     */

                    //Thread.Sleep(1);

                    foreach (string SesssionID in Sessions.IDs) {
                        //Console.WriteLine("[{0}] Target session({1})", ID,SesssionID);
                        TotalBroadcast++;
                        if (ID.Equals(SesssionID))
                        {
                            continue;
                        }
                        if (Subscriptions[SesssionID].ContainsKey(Channel))
                        {
                            //Console.WriteLine("[{0}] Send->{1}", ID, SesssionID);
                            try
                            {
                                TotalSent++;
                                Sockets[SesssionID].Send(Packet.Data);
                            }
                            catch (Exception exception)
                            {
                                Console.WriteLine("[{0}] Exception avoided in send: {1}", ID, exception.Message);
                            }
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

            public channel(string PassedName, bool nyan = false) : this(PassedName)
            {
                flag_nyan = nyan;
            }

            public string name { get; internal set; }
            public bool flag_nyan { get; internal set; }
        }

        /* 
         * Nyan cat for no real reason :)
         */

        public static string nyan()
        {
            return @"
   ,_____ ,              
  ,._ ,_. 7\             
 j `-'     /             
 |o_, o    \             
.`_y_`-,'   !            
|/   `, `._ `-,          
|_     \   _.'*\         
  >--,-'`-'*_*'``---.    
  |\_* _*'-' NYAN    '   
 /    `      UPGRADED \  
 \.         _ .       /  
  '`._     /   )     /   
   \  |`-,-|  /c-'7 /    
    ) \ (_,| |   / (_    
   ((_/   ((_;)  \_)))   
";
        }
    }
}
