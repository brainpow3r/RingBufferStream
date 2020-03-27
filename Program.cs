using RIngBufferStream.Entity;
using System;
using System.IO;
using System.Threading;

namespace RIngBufferStream
{
    public class Program
    {
        public static string _filePathRead = Path.Combine(Directory.GetCurrentDirectory(), "TestFile.txt");
        public static string _filePathWrite = Path.Combine(Directory.GetCurrentDirectory(), "TestFileWrite.txt");
        public static RingBuffer rb = new RingBuffer(1000);
        public static bool _finishedReading = false;

        static void Main(string[] args)
        {

            Thread readerThread = new Thread(ReadFromFile);
            Thread writerThread = new Thread(WriteToFile);

            readerThread.Start();
            writerThread.Start();
            readerThread.Join();
            writerThread.Join();

            Console.WriteLine("RingBuffer state...");
            Console.WriteLine(rb);
        }

        public static void ReadFromFile() {
            using (var fs = new FileStream(_filePathRead, FileMode.Open))
            {
                fs.Seek(3, SeekOrigin.Begin);
                int pos = 0;

                while (pos <= fs.Length-4)
                {
                    pos++;
                    rb.PushFront(fs.ReadByte());
                }
                _finishedReading = true;
            }
        }

        public static void WriteToFile()
        {
            using (var fs = new FileStream(_filePathWrite, FileMode.Open))
            {
                fs.Seek(0, SeekOrigin.Begin);
                while (true) {
                    if (rb.IsEmpty && _finishedReading)
                        break;
                    fs.WriteByte((byte)rb.PopBack());
                }

                Console.WriteLine("FinishedWriting!");
            }

        }

    }
}
