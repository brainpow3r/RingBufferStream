﻿using System;
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
        private bool _finishedReading = false;

        private int InternalIndex(int index) => _head + (index < (Capacity - _head) ? index : index - Capacity); 

        private void ThrowIfEmpty(string message = "Unable to access an empty buffer.")
        {
            if (IsEmpty)
                throw new InvalidOperationException(message);
        }

        private void Increment(ref int index)
        {
            lock(_lock) {
                if (++index == Capacity)
                    index = 0;
            }
        }

        private void Decrement(ref int index)
        {
            lock(_lock) {
                if (index == 0)
                    index = Capacity;

                index--;
            }
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
        public int Size { get { lock (_lock) { return _size; } } }
        public bool IsFull { get { lock (_lock) { return Size == Capacity; } } }
        public bool IsEmpty { get { lock (_lock) { return Size == 0; } } }

        public void PushBack(int item)
        {
            lock (_lock)
            { 
                if (IsFull)
                {
                    Console.WriteLine("Locking PushBack...");
                    _buffer[_tail] = item;
                    Increment(ref _tail);
                    _head = _tail;
                    Monitor.Wait(_lock);
                
                } else
                {
                    _buffer[_tail] = item;
                    Increment(ref _tail);
                    ++_size;
                    Monitor.PulseAll(_lock);
                }
            }
        }

        public void PushFront(int item)
        { 
            lock (_lock)
            {
                if (IsFull)
                {
                    //Console.WriteLine("Locking PushFront...");
                    Monitor.PulseAll(_lock);
                    Decrement(ref _head);
                    _tail = _head;
                    _buffer[_head] = item;
                    Monitor.Wait(_lock);
                }
                else
                {
                    Decrement(ref _head);
                    _buffer[_head] = item;
                    ++_size;
                    Monitor.PulseAll(_lock);
                }
            }
            
        }

        public int PopBack()
        {
            lock(_lock) {

                if (IsEmpty)
                {
                    //Console.WriteLine("Waiting in PopBack...");
                    Monitor.PulseAll(_lock);
                    Monitor.Wait(_lock);
                }

                Decrement(ref _tail);
                int value = _buffer[_tail];
                _buffer[_tail] = default(int);
                --_size;

                Monitor.PulseAll(_lock);
                return value;

            }
        }

        public int PopFront()
        {
            lock(_lock) {
                if (IsEmpty)
                {
                    Console.WriteLine("Waiting in PopFront...");
                    Monitor.Wait(_lock);
                } 

                _buffer[_head] = default(int);
                Increment(ref _head);
                int value = _buffer[_head];
                --_size;

                Monitor.PulseAll(_lock);
                return value;
               
            }
        }

        public override string ToString() => string.Format("[RingBuffer]\nCapaciity: {0}\nSize: {1}\nIsFull: {2}\nIsEmpty: {3}", Capacity, Size, IsFull, IsEmpty);
        
    }
}
