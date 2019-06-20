using System;
using System.Collections.Generic;

namespace CrassusProtocols
{
    public class Protocol
    {

    }

    public class Protocol0 : Protocol
    {
        public uint Version { get; set; }
        public Guid UUID { get; set; }
        public Protocol0 (uint NewVersion,Guid NewUUID)
        {
            Version = NewVersion;
            UUID = NewUUID;
        }
    }

    public class Protocol1 : Protocol
    {
        public string[] Crassus { get; set; }
        public Dictionary<string,string> Routing { get; set; }
        public Dictionary<string,string> Option { get; set; }

        public Protocol1()
        {
            Option = new Dictionary<string, string>();
            Routing = new Dictionary<string, string>();
        }
    }
}
