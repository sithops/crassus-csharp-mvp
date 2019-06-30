using CrassusProtocols;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using WebSocketSharp;

namespace CrassusClasses
{
    public class Plugin
    {

    }

    internal class Switchlist : List<Channel>
    {
        private 
    }

    internal class Channel
    {

    }

    internal class CrassusChannel
    {
        // Cache and primary channel list
        public List<string[]> list { get; set; }
        public bool listBuffer { get; set; }

        // Cache and primary websocket channel store
        public Dictionary<
            string,
            BlockingCollection<WebSocketContainer>
        > websockets { get; set; }
        public bool chanSync { get; set; }
    }
    public class CrassusState
    {
        private Guid guid;
        private string guidAsString;

        // Cache our channels and channel members
        private CrassusChannel channels = new CrassusChannel();

        // Cache of queueSizes
        public int[] queueSizes { get; set; }

        public CrassusState()
        {
            guid = Guid.NewGuid();
            guidAsString = guid.ToString();

            // Initilize the channel buffers
            channels.list = new List<string[]> {
                new string[] { },
                new string[] { }
            };
            channels.websockets = new Dictionary<
                string,
                BlockingCollection<WebSocketContainer>
            >();
        }

        public Guid GetGuid() {
            return guid;
        }

        public string GetGuidAsString()
        {
            return guidAsString;
        }

        internal void AddChannel(string channel)
        {
            channel = channel.ToUpper();

            if (
                channels.listWait.Contains(channel) ||
                channels.websockets.ContainsKey(channel)
            ) {
                return;
            }

            // Add to the cache
            lock (channels.listWait)
            {
                channels.listWait.Add(channel);
                channels.chanSync = false;
            }
        }

        internal string[] ListChannels()
        {
            if (channels.chanSync)
            {
                return channels.list;
            }
            
            return SyncList();
        }

        internal string[] SyncList()
        {
            lock (channels.list)
            {
                channels.list = new string[channels.listWait.Count];
                channels.listWait.CopyTo(channels.list,0);
            }
            return channels.list;
        }
    }

    internal class Worker
    {
        public Thread Thread { get; internal set; }
        public BlockingCollection<(string,string)> Queue { get; internal set; }
        public int ID { get; internal set; }
    }

    internal class WebSocketContainer
    {
        public WebSocket socket { get; internal set; }
        public Guid InternalGuid { get; internal set; }

        // Initial connection detect
        public bool negotiatedVersion { get; set; }
        public uint[] protocolVersionSupport { get; set; }
        public uint protocolVersionLock { get; set; }
        public WebSocketContainer(
            WebSocket newSocket,
            Guid newGuid
        )
        {
            socket = newSocket;
            InternalGuid = newGuid;
        }
    }
}