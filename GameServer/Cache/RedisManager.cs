using System;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace GameServer.Cache
{
    public class RedisManager
    {
        private readonly ConnectionMultiplexer _redis;
        private readonly IDatabase _db;

        public RedisManager(string connectionString)
        {
            _redis = ConnectionMultiplexer.Connect(connectionString);
            _db = _redis.GetDatabase();
            Console.WriteLine("[RedisManager] Connected to Redis DB.");
        }

        // Get Session Data by token directly
        public async Task<(int userId, string username)?> GetSessionByTokenAsync(string token)
        {
            try
            {
                var value = await _db.StringGetAsync($"session:{token}");
                if (value.HasValue)
                {
                    string sessionData = value.ToString();
                    var parts = sessionData.Split(':');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int userId))
                    {
                        return (userId, parts[1]);
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Redis Error] {ex.Message}");
                return null;
            }
        }
    }
}
