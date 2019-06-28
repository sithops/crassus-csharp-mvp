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

    public class CrassusState
    {
        private Guid guid;
        private string guidAsString;

        public CrassusState()
        {
            guid = Guid.NewGuid();
            guidAsString = guid.ToString();
        }

        public Guid GetGuid() {
            return guid;
        }

        public string GetGuidAsString()
        {
            return guidAsString;
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