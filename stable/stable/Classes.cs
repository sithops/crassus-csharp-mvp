using CrassusProtocols;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace CrassusClasses
{
    public class Plugin
    {

    }


    internal class Worker
    {
        public Thread Thread { get; internal set; }
        public BlockingCollection<Protocol[]> Queue { get; internal set; }
        public int ID { get; internal set; }
    }
}