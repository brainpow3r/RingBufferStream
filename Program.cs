using RIngBufferStream.Entity;
using System;

namespace RIngBufferStream
{
    class Program
    {
        static void Main(string[] args)
        {
            RingBuffer<int> rb = new RingBuffer<int>(5);

            rb.PushFront(10);
            rb.PushFront(14);
            rb.PushFront(17);
            rb.PushFront(134);
            rb.PushFront(11);
            rb.PushFront(183);
            rb.PushFront(91);

            var e = rb.GetEnumerator();

            while(e.MoveNext())
            {
                Console.WriteLine("Value: {0}", e.Current);
            }

            Console.WriteLine(rb);
        }
    }
}
