using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using GameServer.Protos;

namespace GameServer.Core
{
    public class Channel
    {
        public int ChannelId { get; private set; }
        private readonly ConcurrentDictionary<int, Player> _players = new();

        public Channel(int id)
        {
            ChannelId = id;
        }

        public async Task JoinAsync(Player player)
        {
            _players.TryAdd(player.Id, player);
            Console.WriteLine($"[Channel {ChannelId}] {player.Username} joined. (Total: {_players.Count})");

            // 1. Tell the new player about all existing players
            foreach (var existingPlayer in _players.Values)
            {
                if (existingPlayer.Id == player.Id) continue;
                
                var spawnExistingForNew = new GamePacket
                {
                    SpawnPlayer = new S2C_SpawnPlayer
                    {
                        PlayerId = existingPlayer.Id,
                        Username = existingPlayer.Username,
                        X = existingPlayer.X,
                        Y = existingPlayer.Y,
                        Z = existingPlayer.Z
                    }
                };
                await player.SendPacketAsync(spawnExistingForNew);
            }

            // 2. Tell existing players about the new player
            var spawnNewForExisting = new GamePacket
            {
                SpawnPlayer = new S2C_SpawnPlayer
                {
                    PlayerId = player.Id,
                    Username = player.Username,
                    X = player.X,
                    Y = player.Y,
                    Z = player.Z
                }
            };
            await BroadcastAsync(spawnNewForExisting, excludePlayerId: player.Id);
        }

        public async Task LeaveAsync(Player player)
        {
            if (_players.TryRemove(player.Id, out _))
            {
                Console.WriteLine($"[Channel {ChannelId}] {player.Username} left. (Total: {_players.Count})");

                var despawnMsg = new GamePacket
                {
                    DespawnPlayer = new S2C_DespawnPlayer { PlayerId = player.Id }
                };
                await BroadcastAsync(despawnMsg, excludePlayerId: player.Id);
            }
        }

        public async Task BroadcastAsync(GamePacket packet, int excludePlayerId = -1)
        {
            var tasks = _players.Values
                .Where(p => p.Id != excludePlayerId)
                .Select(p => p.SendPacketAsync(packet));
            
            await Task.WhenAll(tasks);
        }

        // Accessor to get all players for background tasks (DB Sync)
        public System.Collections.Generic.IEnumerable<Player> GetAllPlayers()
        {
            return _players.Values;
        }
    }
}
