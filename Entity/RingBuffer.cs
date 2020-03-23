using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace RIngBufferStream.Entity
{
    public class RingBuffer
    {
        #region PRIVATE FIELDS
        private readonly int[] _buffer;
        private int _head;
        private int _tail;
        private int _size;
        private string _filePathRead = Path.Combine(Directory.GetCurrentDirectory(), "TestFile.txt");
        private string _filePathWrite = Path.Combine(Directory.GetCurrentDirectory(), "TestFileWrite.txt");
        private object _writerLock = new object();
        private object _readerLock = new object();
        private bool _finishedReading = false;

        private int InternalIndex(int index) => _head + (index < (Capacity - _head) ? index : index - Capacity); 

        private void ThrowIfEmpty(string message = "Unable to access an empty buffer.")
        {
            if (IsEmpty)
                throw new InvalidOperationException(message);
        }

        private void Increment(ref int index)
        {
            if (++index == Capacity)
                index = 0;
        }

        private void Decrement(ref int index)
        {
            if (index == 0)
                index = Capacity;

            index--;
        }



        #endregion

        public RingBuffer(int capacity)
        {
            if (capacity < 1)
                throw new ArgumentException("RingBuffer cannot have negative or zero capacity.", nameof(capacity)); 
            
            _buffer = new int[capacity]; 
            _size = 0;

            _head = 0;
            _tail = 0;

        }

        public int Capacity { get => _buffer.Length; }
        public int Size { get => _size; }
        public bool IsFull { get => Size == Capacity; }
        public bool IsEmpty { get => Size == 0; }

        public int Front()
        {
            ThrowIfEmpty();
            return _buffer[_head];
        }

        public int Back()
        {
            ThrowIfEmpty();
            return _buffer[(_tail != 0 ? _tail : Capacity) - 1];
        }

        public int this[int index]
        {
            get
            {
                if (IsEmpty)
                    throw new IndexOutOfRangeException(string.Format("Cannot access index {0}. Buffer is empty.", index));

                if (index >= _size)
                    throw new IndexOutOfRangeException(string.Format("Cannot access index {0}. Buffer size is {1}", index, _size));

                return _buffer[InternalIndex(index)];
            }

            set
            {
                if (IsEmpty)
                    throw new IndexOutOfRangeException(string.Format("Cannot access index {0}. Buffer is empty.", index));

                if (index >= _size)
                    throw new IndexOutOfRangeException(string.Format("Cannot access index {0}. Buffer size is {1}", index, _size));

                _buffer[InternalIndex(index)] = value;
            }
        }

        public void PushBack(int item)
        {
            if (IsFull)
            {
                _buffer[_tail] = item;
                Increment(ref _tail);
                _head = _tail;
            } else
            {
                _buffer[_tail] = item;
                Increment(ref _tail);
                ++_size;
            }
        }

        public void PushFront(int item)
        { 
            if (IsFull)
            {
                Decrement(ref _head);
                _tail = _head;
                _buffer[_head] = item;
            }
            else
            {
                Decrement(ref _head);
                _buffer[_head] = item;
                ++_size;
            }
            
        }

        public int PopBack()
        {
            ThrowIfEmpty("Cannot Pop elements from an empty buffer.");
            int value = _buffer[_tail];
            _buffer[_tail] = default(int);
            Decrement(ref _tail);
            --_size;
            return value;
        }

        public int PopFront()
        {
            ThrowIfEmpty("Cannot Pop elements from an empty buffer.");
            int value = _buffer[_head];
            _buffer[_head] = default(int);
            Increment(ref _head);
            --_size;
            return value;
        }

        public void ReadFromFile()
        {
            
            using (var fs = new FileStream(_filePathRead, FileMode.Open))
            {
                fs.Seek(0, SeekOrigin.Begin);
                int pos = 0;

                lock(_buffer)
                {
                    while (pos <= fs.Length)
                    {
                        if (IsFull)
                        {
                            Monitor.Wait(_buffer);
                            Monitor.PulseAll(_buffer);
                            Console.WriteLine("Wait in ReadFromFile");
                        }

                        var b = fs.ReadByte();
                        Console.WriteLine("Reading {0} st/th byte...: Symbol: {1}", pos, Convert.ToChar(b));
                        pos++;
                        PushFront(b);

                    }
                    _finishedReading = true;
                }
            }
        }

        public void WriteToFile()
        {
            using (var fs = new FileStream(_filePathWrite, FileMode.Open))
            {

                fs.Seek(0, SeekOrigin.Begin);
                int pos = 0;

                lock (_buffer)
                {
                    while(!_finishedReading)
                    {
                        if (Size == 0)
                        {
                            Monitor.Wait(_buffer);
                        }

                        while (Size > 0)
                        {
                            fs.WriteByte((byte)PopBack());
                        }
                        Monitor.PulseAll(_buffer);
                        Console.WriteLine("PulseAll in WriteToFile");

                    }
                }
            }
        }

        public override string ToString() => string.Format("[RingBuffer]\nCapaciity: {0}\nSize: {1}\nIsFull: {2}\nIsEmpty: {3}", Capacity, Size, IsFull, IsEmpty);
        

        #region IEnumarable<T> implementation
        public IEnumerator<int> GetEnumerator()
        {
            var segments = new ArraySegment<int>[2] { ArrayOne(), ArrayTwo() };
            foreach (ArraySegment<int> segment in segments)
            {
                for (int i = 0; i < segment.Count; i++)
                {
                    yield return segment.Array[segment.Offset + i];
                }
            }
        }

        #endregion

        #region Array items easy access.
        // The array is composed by at most two non-contiguous segments, 
        // the next two methods allow easy access to those.

        private ArraySegment<int> ArrayOne()
        {
            if (IsEmpty)
            {
                return new ArraySegment<int>(Array.Empty<int>());
            }
            else if (_head < _tail)
            {
                return new ArraySegment<int>(_buffer, _head, _tail - _head);
            }
            else
            {
                return new ArraySegment<int>(_buffer, _head, _buffer.Length - _head);
            }
        }

        private ArraySegment<int> ArrayTwo()
        {
            if (IsEmpty)
            {
                return new ArraySegment<int>(Array.Empty<int>());
            }
            else if (_head < _tail)
            {
                return new ArraySegment<int>(_buffer, _tail, 0);
            }
            else
            {
                return new ArraySegment<int>(_buffer, 0, _tail);
            }
        }
        #endregion
    }
}
