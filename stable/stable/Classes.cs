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

    internal class CrassusCache
    {
        // Cache for total channel list
        public string[] channelList { get; set; }
        public Dictionary<string, BlockingCollection<uint>> channelsCache { get; internal set; }
    }
    public class CrassusState
    {
        private Guid guid;
        private string guidAsString;

        // Cache our channels and channel members
        private CrassusCache cacheControl = new CrassusCache();
        // A reference to channels we have defined 
        private Dictionary<string, BlockingCollection<uint>> channels;


        public CrassusState()
        {
            guid = Guid.NewGuid();
            guidAsString = guid.ToString();
            channels = new Dictionary<string, BlockingCollection<uint>>();
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
            if (!channels.ContainsKey(channel.ToUpper()))
            {
                channels.Add(
                    channel,
                    new BlockingCollection<uint>()
                );
            }
        }

        internal string[] ListChannels()
        {
            if (channels.)
            lock (channelList)
            {
                channelList = new string[channels.Count];
                channels.Keys.CopyTo(channelList, 0);
            }
            return channelList;
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
        public WebSocketContainer(WebSocket newSocket,Guid newGuid)
        {
            socket = newSocket;
            InternalGuid = newGuid;
        }
    }
}