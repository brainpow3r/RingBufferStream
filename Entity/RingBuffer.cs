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
        private object _lock = new Object();
        private bool _finalize = false;

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

        public void Insert(int item)
        { 
            lock (_lock)
            {
                while (IsFull)
                {
                    Monitor.PulseAll(_lock);
                    Monitor.Wait(_lock);
                }

                Decrement(ref _head);
                _buffer[_head] = item;
                ++_size;
                Monitor.PulseAll(_lock);

            }
            
        }

        public int Take(bool finished)
        {
            lock(_lock) {

                if (IsEmpty)
                {
                    Monitor.PulseAll(_lock);
                    Monitor.Wait(_lock);
                    Console.WriteLine("Waiting");
                }

                Decrement(ref _tail);
                int value = _buffer[_tail];
                _buffer[_tail] = default(int);
                --_size;

                Monitor.PulseAll(_lock);
                return value;

            }
        }

        public override string ToString() => string.Format("[RingBuffer]\nCapaciity: {0}\nSize: {1}\nIsFull: {2}\nIsEmpty: {3}", Capacity, Size, IsFull, IsEmpty);
        
    }
}
