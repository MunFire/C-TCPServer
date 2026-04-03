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
        public readonly ConcurrentDictionary<int, Monster> _monsters = new();

        private readonly CancellationTokenSource _cts = new();

        public Channel(int id)
        {
            ChannelId = id;
            
            // Spawn some default monsters for this channel
            // Random positions around the origin. Parameters: id, type, x, y, z, isAggressive
            _monsters.TryAdd(1, new Monster(1, 0, 10f, 0f, 10f, false)); // Normal (Counter-attack only)
            _monsters.TryAdd(2, new Monster(2, 0, -10f, 0f, 10f, true));  // Normal (Aggressive)
            _monsters.TryAdd(3, new Monster(3, 0, 10f, 0f, -10f, false)); // Normal (Counter-attack only)
            _monsters.TryAdd(4, new Monster(4, 1, 0f, 0f, 0f, true));   // Boss (Aggressive)

            // Start AI Background loop
            _ = Task.Run(() => ChannelUpdateLoopAsync(_cts.Token));
        }

        private async Task ChannelUpdateLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    foreach (var monster in _monsters.Values)
                    {
                        await monster.UpdateAsync(this);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Channel {ChannelId}] Exception in Update Loop: {ex.Message}");
                }

                await Task.Delay(500, token); // Update twice per second
            }
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

            // 1.5. Tell the new player about all existing monsters
            foreach (var monster in _monsters.Values)
            {
                if (monster.IsDead) continue;
                
                var spawnMonsterPkt = new GamePacket
                {
                    SpawnMonster = new S2C_SpawnMonster
                    {
                        MonsterId = monster.Id,
                        MonsterType = monster.MonsterType,
                        Hp = monster.Hp,
                        MaxHp = monster.MaxHp,
                        X = monster.X,
                        Y = monster.Y,
                        Z = monster.Z
                    }
                };
                await player.SendPacketAsync(spawnMonsterPkt);
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

        public Player GetPlayer(int id)
        {
            _players.TryGetValue(id, out var p);
            return p;
        }

        public void Stop()
        {
            _cts.Cancel();
        }
    }
}
