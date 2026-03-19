using StackExchange.Redis;
using System.Threading.Tasks;
using System;

namespace AuthServer.Cache
{
    public class RedisManager
    {
        private readonly ConnectionMultiplexer? _redis;
        private readonly IDatabase? _db;

        public RedisManager(string connectionString)
        {
            try
            {
                _redis = ConnectionMultiplexer.Connect(connectionString);
                _db = _redis.GetDatabase();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Redis Error] Failed to connect: {ex.Message}");
            }
        }

        public async Task SaveSessionAsync(string sessionData, string token, TimeSpan expiry)
        {
            if (_db == null) return;
            // Store sessionData (userId:username) against the token
            await _db.StringSetAsync($"session:{token}", sessionData, expiry);
        }

        public async Task<string?> GetSessionAsync(string token)
        {
            if (_db == null) return null;
            // Get sessionData (userId:username) by token
            var value = await _db.StringGetAsync($"session:{token}");
            return value.HasValue ? value.ToString() : null;
        }

        public async Task DeleteSessionAsync(string token)
        {
            if (_db == null) return;
            await _db.KeyDeleteAsync($"session:{token}");
        }
    }
}
