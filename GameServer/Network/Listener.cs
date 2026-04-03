using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace GameServer.Network
{
    public class Listener
    {
        private Socket _listenSocket;
        private readonly int _port;
        private readonly BufferManager _bufferManager;
        private readonly SocketAsyncEventArgsPool _recvPool;
        private readonly SocketAsyncEventArgsPool _sendPool;
        
        // Callback when a client connects and is wrapped in a Session
        public Action<Session> OnClientConnected { get; set; }

        public Listener(int port, int maxConnections, int bufferSize)
        {
            _port = port;
            
            // Number of connections * 2 (because one for send, one for recv)
            // But we actually only allocate buffer for receive pool if we use it directly.
            _bufferManager = new BufferManager(bufferSize * maxConnections, bufferSize);
            _recvPool = new SocketAsyncEventArgsPool(maxConnections);
            _sendPool = new SocketAsyncEventArgsPool(maxConnections);

            for (int i = 0; i < maxConnections; i++)
            {
                var recvArgs = new SocketAsyncEventArgs();
                recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                _bufferManager.SetBuffer(recvArgs);
                _recvPool.Push(recvArgs);

                var sendArgs = new SocketAsyncEventArgs();
                sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                // We'll not set fixed buffer for sendArgs, we assign dynamically per packet
                _sendPool.Push(sendArgs);
            }
        }

        public void Start()
        {
            _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(new IPEndPoint(IPAddress.Any, _port));
            _listenSocket.Listen(100);

            Console.WriteLine($"[Listener] SAEA Listener Started on port {_port}");
            
            StartAccept(null);
        }

        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            }
            else
            {
                acceptEventArg.AcceptSocket = null;
            }

            bool willRaiseEvent = _listenSocket.AcceptAsync(acceptEventArg);
            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArg);
            }
        }

        private void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                Socket clientSocket = e.AcceptSocket;

                var recvArgs = _recvPool.Pop();
                var sendArgs = _sendPool.Pop();

                if (recvArgs != null && sendArgs != null)
                {
                    Session session = new Session();
                    session.OnDisconnected += OnSessionDisconnected;
                    
                    // Attach event handlers inside OnClientConnected callback if needed, then Start
                    OnClientConnected?.Invoke(session);

                    session.Start(clientSocket, recvArgs, sendArgs);
                }
                else
                {
                    Console.WriteLine("[Listener] Maximum connections reached.");
                    clientSocket.Close();
                }
            }

            // Accept the next connection request
            StartAccept(e);
        }

        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.UserToken is Session session)
            {
                switch (e.LastOperation)
                {
                    case SocketAsyncOperation.Receive:
                        session.ProcessReceive(e);
                        break;
                    case SocketAsyncOperation.Send:
                        // ProcessSend
                        break;
                    default:
                        throw new ArgumentException("The last operation completed on the socket was not a receive or send");
                }
            }
        }

        private void OnSessionDisconnected(Session session)
        {
            if (session.RecvArgs != null)
                _recvPool.Push(session.RecvArgs);
            
            if (session.SendArgs != null)
                _sendPool.Push(session.SendArgs);
                
            Console.WriteLine($"[Listener] Session disconnected. SAEA returned to pool. Free count: {_recvPool.Count}");
        }
    }
}
