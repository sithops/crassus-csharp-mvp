using System;
using System.Collections.Generic;
using WebSocketSharp;
using WebSocketSharp.Server;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Threading;
using stable;

namespace Crassus
{
    public class Program
    {
        // Number of workers to use
        const int WorkerCount = 8;
        const int BufferSize = 1024;

        static ConcurrentQueue<Packet> InboundPackets = new ConcurrentQueue<Packet>();
        static int[] QueueSizes;
        static List<Worker> Workers = new List<Worker>();

        public static void Main(string[] args)
        {
            uint PacketID   = 0;
            int WorkerID    = 0;

            QueueSizes = new int[WorkerCount];

            DateTime StartTime = DateTime.Now;

            for (
                int Iterator = 0; 
                Iterator < WorkerCount; 
                Iterator++
            )
            {
                QueueSizes[Iterator] = 0;

                Worker Worker   = new Worker();
                Worker.Thread   = new Thread(new ParameterizedThreadStart(Consumer));
                Worker.Queue    = new BlockingCollection<Packet>();
                Worker.ID       = WorkerID;
                Worker.Thread.Start(WorkerID++);
                Workers.Add(Worker);
            }

            // Feed the wheel
            while(true)
            {
                for (
                    int Iterator = 0;
                    Iterator < WorkerCount;
                    Iterator++
                )
                {
                    int FillDiff = BufferSize - QueueSizes[Iterator];
                    for (
                        int BufferFillIterator = 0;
                        BufferFillIterator < FillDiff;
                        BufferFillIterator++
                    )
                    {
                        Packet ExamplePacket = new Packet();
                        ExamplePacket.id = PacketID++;
                        Workers[Iterator].Queue.Add(ExamplePacket);
                    }
                }

                if ((DateTime.Now - StartTime).TotalSeconds >= 60)
                {
                    Console.WriteLine("Processed: {0} sends in 60 seconds",PacketID);
                    break;
                }

                Thread.Sleep(1);
            }

            // In the main thread we will do some shit?
            Console.ReadKey();
        }

        static void Consumer(Object MyID)
        {
            int ID = (int)MyID;

            Worker Workload = new Worker();
            Console.WriteLine("Thread started");

            while (true) {
                if (Workers[ID].Queue.TryTake(out Packet DPkt))
                {
                    // Console.WriteLine("Read packet id: {0}", DPkt.id);
                }
            }
        }
    }
    internal class Plugin
    {

    }
    internal class Packet
    {
        internal uint id;
    }
    internal class Worker
    {
        public Thread Thread { get; internal set; }
        public BlockingCollection<Packet> Queue { get; internal set; }
        public int ID { get; internal set; }
    }
}
