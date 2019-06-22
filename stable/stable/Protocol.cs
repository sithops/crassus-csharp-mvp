using System;
using System.Collections.Generic;

namespace CrassusProtocols
{
    public class Protocol
    {

    }

    public class PacketVersion : Protocol
    {
        private readonly List<uint> Supported = new List<uint>() {
            0,
            1
        };
        private readonly uint SupportedMax = 1;
        /// <summary>
        ///  Supports queries the Protocol versios availible in the library
        /// </summary>
        /// <param name="Comparator">What version to check is availible</param>
        /// <returns>bool</returns>
        public bool Supports (uint Comparator)
        {
            return Supported.Contains(Comparator) ? true : false;
        }
        public uint Max ()
        {
            return SupportedMax;
        }

        public uint[] AsArray()
        {
            return Supported.ToArray();
        }

        public uint[] Closest (uint Comparator)
        {
            // Maybe we are on the same level anyway?
            uint MaxSupported = Max();
            if (MaxSupported == Comparator)
            {
                return new uint[] { Comparator };
            }
            else if (MaxSupported < Comparator)
            {
                return new uint[] { MaxSupported };
            }
            else
            {
                // Start at our maximum and work backwards
                for (
                    uint i = Comparator;
                    i > 0;
                    i--
                )
                {
                    if (Supports(i))
                    {
                        return new uint[] { i };
                    }
                }
                // No closest match!
                return new uint[] { };
            }
        }
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
