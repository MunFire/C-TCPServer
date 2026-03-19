using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameServer.Protos;
using GameServer.Database;

namespace GameServer.Core
{
    public class ChannelManager
    {
        private readonly ConcurrentDictionary<int, Channel> _channels = new();
        private readonly MySqlManager _dbManager;
        private Timer _dbSyncTimer;

        public ChannelManager(MySqlManager dbManager)
        {
            _dbManager = dbManager;
            
            // Initialize with e.g. 5 default channels
            for (int i = 1; i <= 5; i++)
            {
                _channels.TryAdd(i, new Channel(i));
            }

            // Set up background DB sync (10 minutes = 600,000 ms)
            // Fire first time after 10m, then every 10m.
            _dbSyncTimer = new Timer(SyncDatabase, null, 600_000, 600_000);
        }

        private async void SyncDatabase(object? state)
        {
            Console.WriteLine($"[ChannelManager] Starting scheduled DB Sync for all players...");
            int count = 0;
            foreach (var channel in _channels.Values)
            {
                foreach (var player in channel.GetAllPlayers())
                {
                    await _dbManager.SaveCharacterAsync(player.Username, player.Hp, player.Level, player.Exp);
                    count++;
                }
            }
            Console.WriteLine($"[ChannelManager] DB Sync complete. Saved {count} players.");
        }

        public Channel GetChannel(int channelId)
        {
            if (_channels.TryGetValue(channelId, out var channel))
            {
                return channel;
            }
            // Fallback to Channel 1 if requested channel doesn't exist
            return _channels[1];
        }

        public Channel GetDefaultChannel()
        {
            return _channels[1];
        }

        public async Task BroadcastToAllChannelsAsync(GamePacket packet)
        {
            var tasks = _channels.Values.Select(c => c.BroadcastAsync(packet));
            await Task.WhenAll(tasks);
        }
    }
}
