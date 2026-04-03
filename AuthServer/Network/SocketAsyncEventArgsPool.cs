using System;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace AuthServer.Network
{
    public class SocketAsyncEventArgsPool
    {
        private readonly ConcurrentStack<SocketAsyncEventArgs> _pool;
        
        public int Count => _pool.Count;

        public SocketAsyncEventArgsPool(int capacity)
        {
            _pool = new ConcurrentStack<SocketAsyncEventArgs>();
        }

        public void Push(SocketAsyncEventArgs item)
        {
            if (item == null) 
                throw new ArgumentNullException(nameof(item));
                
            _pool.Push(item);
        }

        public SocketAsyncEventArgs Pop()
        {
            if (_pool.TryPop(out SocketAsyncEventArgs args))
                return args;
            return null;
        }
    }
}
