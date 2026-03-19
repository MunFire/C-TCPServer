using System;
using System.Threading.Tasks;
using GameServer.Network;
using GameServer.Protos;
using Google.Protobuf;

namespace GameServer.Core
{
    public class Player
    {
        public int Id { get; private set; }
        public string Username { get; private set; }
        
        // Transform
        public float X { get; set; } = 0f;
        public float Y { get; set; } = 0f;
        public float Z { get; set; } = 0f;

        // Stats
        public int Hp { get; set; } = 100;
        public int Level { get; set; } = 1;
        public int Exp { get; set; } = 0;

        private readonly ClientHandler _session;

        public Player(int id, string username, ClientHandler session)
        {
            Id = id;
            Username = username;
            _session = session;
        }

        public async Task SendPacketAsync(GamePacket packet)
        {
            await _session.SendPacketAsync(packet);
        }
    }
}
