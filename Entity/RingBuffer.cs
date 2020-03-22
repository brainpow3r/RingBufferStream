using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace RIngBufferStream.Entity
{
    public class RingBuffer<T> : IEnumerable<T>
    {
        #region PRIVATE FIELDS
        private readonly T[] _buffer;
        private int _head;
        private int _tail;
        private int _size;
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
            
            _buffer = new T[capacity]; 
            _size = 0;

            _head = 0;
            _tail = 0;

        }

        public int Capacity { get => _buffer.Length; }
        public int Size { get => _size; }
        public bool IsFull { get => Size == Capacity; }
        public bool IsEmpty { get => Size == 0; }

        public T Front()
        {
            ThrowIfEmpty();
            return _buffer[_head];
        }

        public T Back()
        {
            ThrowIfEmpty();
            return _buffer[(_tail != 0 ? _tail : Capacity) - 1];
        }

        public T this[int index]
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

        public void PushBack(T item)
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

        public void PushFront(T item)
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

        public void PopBack()
        {
            ThrowIfEmpty("Cannot Pop elements from an empty buffer.");
            Decrement(ref _tail);
            _buffer[_tail] = default(T);
            --_size;
        }

        public void PopFront()
        {
            ThrowIfEmpty("Cannot Pop elements from an empty buffer.");
            _buffer[_head] = default(T);
            Increment(ref _head);
            --_size;
        }

        public override string ToString() => string.Format("[RingBuffer]\nCapaciity: {0}\nSize: {1}\nIsFull: {2}\nIsEmpty: {3}", Capacity, Size, IsFull, IsEmpty);
        

        #region IEnumarable<T> implementation
        public IEnumerator<T> GetEnumerator()
        {
            var segments = new ArraySegment<T>[2] { ArrayOne(), ArrayTwo() };
            foreach (ArraySegment<T> segment in segments)
            {
                for (int i = 0; i < segment.Count; i++)
                {
                    yield return segment.Array[segment.Offset + i];
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => (IEnumerator)GetEnumerator();
        #endregion

        #region Array items easy access.
        // The array is composed by at most two non-contiguous segments, 
        // the next two methods allow easy access to those.

        private ArraySegment<T> ArrayOne()
        {
            if (IsEmpty)
            {
                return new ArraySegment<T>(Array.Empty<T>());
            }
            else if (_head < _tail)
            {
                return new ArraySegment<T>(_buffer, _head, _tail - _head);
            }
            else
            {
                return new ArraySegment<T>(_buffer, _head, _buffer.Length - _head);
            }
        }

        private ArraySegment<T> ArrayTwo()
        {
            if (IsEmpty)
            {
                return new ArraySegment<T>(Array.Empty<T>());
            }
            else if (_head < _tail)
            {
                return new ArraySegment<T>(_buffer, _tail, 0);
            }
            else
            {
                return new ArraySegment<T>(_buffer, 0, _tail);
            }
        }
        #endregion
    }
}
