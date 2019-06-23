using CrassusProtocols;
using System;
using System.Collections.Concurrent;
using System.Threading;
using WebSocketSharp;

namespace CrassusClasses
{
    public class Plugin
    {

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
        public Guid internalGuid { get; internal set; }

        // Initial connection detect
        public bool negotiatedVersion { get; set; }
        public uint[] protocolVersionSupport { get; set; }

        public WebSocketContainer(WebSocket newSocket,Guid newGuid)
        {
            socket = newSocket;
            internalGuid = newGuid;
        }
    }
}