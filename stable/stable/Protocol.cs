using System;
using System.Collections.Generic;

namespace CrassusProtocols
{
    public class Protocol
    {

    }

    public class Version : Protocol
    {
        public readonly List<uint> Supported = new List<uint>() {
            0,
            1
        };
    }

    public class Protocol0 : Protocol
    {
        public uint version { get; set; }
        public Guid uuid { get; set; }
        public Protocol0 (uint NewVersion,Guid NewUUID)
        {
            version = NewVersion;
            uuid = NewUUID;
        }
    }

    public class Protocol1 : Protocol
    {
        public uint[] crassus { get; set; }
    }
    public class Protocol100 : Protocol
    {
        public string[] crassus { get; set; }
        public Dictionary<string,object> routing { get; set; }
        public Dictionary<string,string> option { get; set; }

        public Protocol100()
        {
            option = new Dictionary<string, string>();
            routing = new Dictionary<string, object>();
        }
    }
}
