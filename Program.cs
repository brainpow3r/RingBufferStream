using RIngBufferStream.Entity;
using System;
using System.IO;
using System.Threading;

namespace RIngBufferStream
{
    internal class Program
    {
        static void Main(string[] args)
        {
            RingBuffer rb = new RingBuffer(10);

            Thread readerThread = new Thread(rb.ReadFromFile);
            Thread writerThread = new Thread(rb.WriteToFile);

            readerThread.Start();
            writerThread.Start();
            readerThread.Join();
            writerThread.Join();

            var e = rb.GetEnumerator();

            while(e.MoveNext())
            {
                Console.WriteLine("Value: {0}", e.Current);
            }

            Console.WriteLine(rb);
        }

    }
}
