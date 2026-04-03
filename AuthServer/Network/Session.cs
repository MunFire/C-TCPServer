using System;
using System.Collections.Generic;
using System.Net.Sockets;
using AuthServer.Protos;
using Google.Protobuf;

namespace AuthServer.Network
{
    public class Session
    {
        public Socket Socket { get; private set; }
        
        public SocketAsyncEventArgs RecvArgs { get; private set; }
        public SocketAsyncEventArgs SendArgs { get; private set; }
        
        private readonly List<byte> _recvBufferList = new();
        
        public Action<Packet, Session> OnPacketReceived { get; set; }
        public Action<Session> OnDisconnected { get; set; }

        private bool _isDisconnected = false;
        private readonly object _disconnectLock = new object();

        public Session()
        {
        }

        public void Start(Socket socket, SocketAsyncEventArgs recvArgs, SocketAsyncEventArgs sendArgs)
        {
            Socket = socket;
            RecvArgs = recvArgs;
            SendArgs = sendArgs;

            RecvArgs.UserToken = this;
            SendArgs.UserToken = this;

            _isDisconnected = false;
            _recvBufferList.Clear();

            RegisterReceive();
        }

        private void RegisterReceive()
        {
            if (_isDisconnected) return;

            try
            {
                bool pending = Socket.ReceiveAsync(RecvArgs);
                if (!pending)
                {
                    ProcessReceive(RecvArgs);
                }
            }
            catch (Exception)
            {
                Disconnect();
            }
        }

        public void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                lock (_recvBufferList)
                {
                    for (int i = 0; i < e.BytesTransferred; i++)
                    {
                        _recvBufferList.Add(e.Buffer[e.Offset + i]);
                    }

                    while (_recvBufferList.Count > 0)
                    {
                        if (!TryReadVarint(_recvBufferList, out int length, out int varintSize))
                            break; 

                        if (_recvBufferList.Count < varintSize + length)
                            break; 

                        byte[] payload = new byte[length];
                        _recvBufferList.CopyTo(varintSize, payload, 0, length);
                        
                        try
                        {
                            var packet = Packet.Parser.ParseFrom(payload);
                            OnPacketReceived?.Invoke(packet, this);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Session] Protobuf Parse Error: {ex.Message}");
                            Disconnect();
                            return;
                        }

                        _recvBufferList.RemoveRange(0, varintSize + length);
                    }
                }

                RegisterReceive();
            }
            else
            {
                Disconnect();
            }
        }

        private bool TryReadVarint(List<byte> buffer, out int length, out int varintSize)
        {
            length = 0;
            varintSize = 0;
            for (int i = 0; i < buffer.Count; i++)
            {
                byte b = buffer[i];
                length |= (b & 0x7F) << (7 * i);
                varintSize++;

                if ((b & 0x80) == 0) 
                {
                    return true;
                }
                
                if (i >= 4) return false; 
            }
            return false;
        }

        public void Send(Packet packet)
        {
            if (_isDisconnected) return;

            try
            {
                int msgSize = packet.CalculateSize();
                int varintSize = CodedOutputStream.ComputeLengthSize(msgSize);
                
                byte[] sendData = new byte[varintSize + msgSize];
                var cos = new CodedOutputStream(sendData);
                cos.WriteLength(msgSize);
                packet.WriteTo(cos);
                cos.Flush();

                Socket.Send(sendData);
            }
            catch (Exception)
            {
                Disconnect();
            }
        }

        public void Disconnect()
        {
            lock (_disconnectLock)
            {
                if (_isDisconnected) return;
                _isDisconnected = true;
            }

            try
            {
                Socket?.Shutdown(SocketShutdown.Both);
            }
            catch { }

            try
            {
                Socket?.Close();
            }
            catch { }

            OnDisconnected?.Invoke(this);
        }
    }
}
