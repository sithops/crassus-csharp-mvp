using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace CrassusProtocols
{
    public class Protocol
    {

    }

    public class PacketVersion : Protocol
    {
        private readonly List<uint> supportedVersions = new List<uint>() {
            0
        };
        private readonly uint supportedMax = 1;
        /// <summary>
        ///  Supports queries the Protocol versios availible in the library
        /// </summary>
        /// <param name="Comparator">What version to check is availible</param>
        /// <returns>bool</returns>
        public bool Supports (uint Comparator)
        {
            return supportedVersions.Contains(Comparator) ? true : false;
        }
        public uint Max ()
        {
            return supportedMax;
        }

        public uint[] ToArray()
        {
            return supportedVersions.ToArray();
        }

        public uint[] Closest (uint comparator)
        {
            // Maybe we are on the same level anyway?
            uint maxSupported = Max();
            if (maxSupported == comparator)
            {
                return new uint[] { comparator };
            }
            else if (maxSupported < comparator)
            {
                return new uint[] { maxSupported };
            }
            else
            {
                // Start at our maximum and work backwards
                for (
                    uint i = comparator;
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
        /// <summary>
        /// Compare a list of versions to the versions crassus supports and return a sorted list matches, 
        /// Optionally sort in Descending order by setting param2 as true
        /// </summary>
        /// <param name="PluginVersions">A list of versions offered by a plugin or client</param>
        /// <param name="Reverse">Reverse the sort to be Descending</param>
        /// <returns>A list of versions</returns>
        internal uint[] Negotiate(
            List<uint> PluginVersions,
            bool Reverse = false
        )
        {
            List<uint> MatchedVersions = new List<uint>();
            foreach (uint ClientVersion in PluginVersions)
            {
                if (supportedVersions.Contains(ClientVersion)) {
                    MatchedVersions.Add(ClientVersion);
                }
            }
            MatchedVersions.Sort();

            if (Reverse)
            {
                MatchedVersions.Reverse();
            }

            return MatchedVersions.ToArray();
        }
    }
    /// <summary>
    /// Cast and access version 0 packets, requires a version(uint)
    /// and optionally a Guid, if no Guid is specified one will be 
    /// automatically created
    /// </summary>
    /// 
    //A class should either have a default constructor, one constructor with arguments or a constructor marked with the JsonConstructor attribute.Path '[0].version', line 1, position 12.'
    public class ProtocolHeader0 : Protocol
    {
        public uint version { get; set; }
        public Guid uuid { get; set; }
        [JsonConstructor]
        public ProtocolHeader0()
        {
            //version = (uint)newVersion;
            //uuid = Guid.Parse(newUUID);
        }
        public ProtocolHeader0(uint newVersion,Guid newUUID)
        {
            version = newVersion;
            uuid = newUUID;
        }
        public ProtocolHeader0(uint NewVersion)
        {
            version = NewVersion;
            uuid = Guid.NewGuid();
        }
    }
    /// <summary>
    /// Cast and access version 1 packets
    /// </summary>
    public class ProtocolBody0 : Protocol
    {
        public byte[] crassus { get; set; }
        public Dictionary<string, Guid> routing { get; set; }
        public string tag { get; set; }
        public Dictionary<string,string> option { get; set; }
        public ProtocolBody0()
        {
            option = new Dictionary<string, string>();
            routing = new Dictionary<string, Guid>();
        }
    }
    /// <summary>
    /// Cast and access ProtocolX packets (Internal communication only)
    /// </summary>
    public class ProtocolX : Protocol
    {
        public ProtocolX(uint[] protocolVersionSupport)
        {
            versions = protocolVersionSupport;
        }

        public uint[] versions { get; set; }
    }
}
