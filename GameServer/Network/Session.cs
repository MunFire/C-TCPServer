using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GameServer.Protos;
using Google.Protobuf;

namespace GameServer.Network
{
    public class Session
    {
        public Socket Socket { get; private set; }
        
        public SocketAsyncEventArgs RecvArgs { get; private set; }
        public SocketAsyncEventArgs SendArgs { get; private set; }
        
        private readonly List<byte> _recvBufferList = new();
        
        // Callback when a complete Protobuf packet is assembled
        public Action<GamePacket, Session> OnPacketReceived { get; set; }
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
                    // 1. Copy received bytes to internal list buffer
                    for (int i = 0; i < e.BytesTransferred; i++)
                    {
                        _recvBufferList.Add(e.Buffer[e.Offset + i]);
                    }

                    // 2. Parse complete packets
                    while (_recvBufferList.Count > 0)
                    {
                        if (!TryReadVarint(_recvBufferList, out int length, out int varintSize))
                            break; // Not enough bytes for varint length

                        if (_recvBufferList.Count < varintSize + length)
                            break; // Not enough bytes for the whole packet payload

                        // Extract bytes
                        byte[] payload = new byte[length];
                        _recvBufferList.CopyTo(varintSize, payload, 0, length);
                        
                        // Parse Protobuf
                        try
                        {
                            var packet = GamePacket.Parser.ParseFrom(payload);
                            OnPacketReceived?.Invoke(packet, this);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Session] Protobuf Parse Error: {ex.Message}");
                            Disconnect();
                            return;
                        }

                        // Shift buffer
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

                if ((b & 0x80) == 0) // End of varint
                {
                    return true;
                }
                
                if (i >= 4) return false; // Max 5 bytes for 32-bit varint
            }
            return false;
        }

        public void Send(GamePacket packet)
        {
            if (_isDisconnected) return;

            try
            {
                // Serialize packet with varint length prefix using CodedOutputStream
                // Calculate total size: varint_size + msg_size
                int msgSize = packet.CalculateSize();
                int varintSize = CodedOutputStream.ComputeLengthSize(msgSize);
                
                byte[] sendData = new byte[varintSize + msgSize];
                var cos = new CodedOutputStream(sendData);
                cos.WriteLength(msgSize);
                packet.WriteTo(cos);
                cos.Flush();

                // Standard send (we can optimize this by putting into a queue and using _sendArgs,
                // but direct socket send is fine for immediate testing since sendArgs pooling requires send queues)
                // For simplicity of SAEA, let's use standard BeginSend or SendAsync for now, 
                // but if we are restricted to _sendArgs we'd need a queue. 
                // We'll use TAP for Send to prevent buffer overlay.
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
