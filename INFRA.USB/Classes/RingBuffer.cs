using System;
using System.Threading;

namespace INFRA.USB
{
    public class RingBuffer<T> where T : new()
    {
        private int _readIndex;
        private int _writeIndex;
        private int _lengthToRead;
        private readonly T[] _buffer;
        private readonly int _bufferSize;
        private readonly int _requiredLength;
        private readonly object _lockObject = new object();

        public EventHandler DataAvilable;

        public RingBuffer(int size)
        {
            _bufferSize = size;
            _requiredLength = 0;
            _buffer = new T[_bufferSize];
            for (int i = 0; i < _bufferSize; i++)
            {
                _buffer[i] = new T();
            }
        }

        public RingBuffer(int size, int requiredLengthBeforeEvent)
        {
            _bufferSize = size;
            _requiredLength = requiredLengthBeforeEvent;
            _buffer = new T[_bufferSize];
            for (int i = 0; i < _bufferSize; i++)
            {
                _buffer[i] = new T();
            }
        }

        public void Reset()
        {
            lock (_lockObject)
            {
                _readIndex = 0;
                _writeIndex = 0;
                _lengthToRead = 0;
            }
        }

        public int LengthToRead
        {
            private set
            {
                lock (_lockObject)
                {
                    _lengthToRead = value;
                    if (_requiredLength != 0 && _lengthToRead == _requiredLength)
                    {
                        if (DataAvilable != null)
                        {
                            DataAvilable(this, EventArgs.Empty);
                        }   
                    }
                }
            }

            get
            {
                lock (_lockObject)
                {
                    return _lengthToRead;
                }
            }
        }

        public int RemainingLengthToWrite
        {
            get
            {
                lock (_lockObject)
                {
                    return _bufferSize - LengthToRead;
                }
            }
        }

        public void PutOverwriting(T data)
        {
            lock (_lockObject)
            {
                _buffer[_writeIndex] = data;
                LengthToRead = (LengthToRead + 1)% _bufferSize;
                _writeIndex = (_writeIndex + 1) % _bufferSize;
                Monitor.Pulse(_lockObject);
            }
        }

        public void PutOverwriting(T[] data, int startIndex, int length)
        {
            lock (_lockObject)
            {
                for (int i = 0; i < length; i++)
                {
                    _buffer[_writeIndex] = data[startIndex + i];
                    LengthToRead = (LengthToRead + 1) % _bufferSize;
                    _writeIndex = (_writeIndex + 1) % _bufferSize;
                    Monitor.Pulse(_lockObject);
                }
            }
        }

        public void PutBlocking(T data)
        {
            lock (_lockObject)
            {
                while (LengthToRead == _bufferSize)
                {
                    Monitor.Wait(_lockObject);
                }
                _buffer[_writeIndex] = data;
                _writeIndex = (_writeIndex + 1)%_bufferSize;
                LengthToRead++;
                Monitor.Pulse(_lockObject);
            }
        }

        public void PutBlocking(T[] data, int startIndex, int length)
        {
            lock (_lockObject)
            {
                for (int i = 0; i < length; i++)
                {
                    while (LengthToRead == _bufferSize)
                    {
                        Monitor.Wait(_lockObject, 100);
                    }
                    _buffer[_writeIndex] = data[startIndex + i];
                    _writeIndex = (_writeIndex + 1)%_bufferSize;
                    LengthToRead++;
                    Monitor.Pulse(_lockObject);
                }
            }
        }

        public void GetNormal(ref T data)
        {
            lock (_lockObject)
            {
                if (LengthToRead == 0)
                {
                    return;
                }
                data = _buffer[_readIndex];
                _readIndex = (_readIndex + 1)%_bufferSize;
                LengthToRead--;
                Monitor.Pulse(_lockObject);
            }
        }

        public void GetNormal(T[] data, int length)
        {
            lock (_lockObject)
            {
                for (int i = 0; i < length; i++)
                {
                    if (LengthToRead == 0)
                    {
                        Monitor.Wait(_lockObject);
                    }
                    data[i] = _buffer[_readIndex];
                    _readIndex = (_readIndex + 1)%_bufferSize;
                    LengthToRead--;
                    Monitor.Pulse(_lockObject);
                }
            }
        }

        public void GetBlocking(ref T data)
        {
            lock (_lockObject)
            {
                while (LengthToRead == 0)
                {
                    Monitor.Wait(_lockObject);
                }
                data = _buffer[_readIndex];
                _readIndex = (_readIndex + 1)%_bufferSize;
                LengthToRead--;
                Monitor.Pulse(_lockObject);
            }
        }

        public void GetBlocking(T[] data, int length)
        {
            lock (_lockObject)
            {
                for (int i = 0; i < length; i++)
                {
                    while (LengthToRead == 0)
                    {
                        Monitor.Wait(_lockObject, 100);
                    }
                    data[i] = _buffer[_readIndex];
                    _readIndex = (_readIndex + 1)%_bufferSize;
                    LengthToRead--;
                    Monitor.Pulse(_lockObject);
                }
            }
        }
    }
}
