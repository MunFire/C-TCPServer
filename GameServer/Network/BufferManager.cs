using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace GameServer.Network
{
    public class BufferManager
    {
        private readonly int _numBytes;                 // Total buffer size
        private readonly byte[] _buffer;                // The giant underlying buffer representing the pool
        private readonly Stack<int> _freeIndexPool;     // Free index locations
        private int _currentIndex;
        private readonly int _bufferSize;               // Size of each separate buffer slice

        public BufferManager(int totalBytes, int bufferSize)
        {
            _numBytes = totalBytes;
            _currentIndex = 0;
            _bufferSize = bufferSize;
            _freeIndexPool = new Stack<int>();
            _buffer = new byte[_numBytes];
        }

        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            if (_freeIndexPool.Count > 0)
            {
                // Give back an already used and freed chunk
                args.SetBuffer(_buffer, _freeIndexPool.Pop(), _bufferSize);
                return true;
            }
            
            if ((_numBytes - _bufferSize) < _currentIndex)
            {
                return false; // Out of memory buffer space
            }

            // Assign a new slice of the giant buffer
            args.SetBuffer(_buffer, _currentIndex, _bufferSize);
            _currentIndex += _bufferSize;
            return true;
        }

        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            _freeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0); // Remove reference
        }
    }
}
