using System;
using System.Threading;
using System.Threading.Tasks;
using GameServer.Cache;
using GameServer.Network;
using GameServer.Database;

namespace GameServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("    GameServer - In-Game Logic Core     ");
            Console.WriteLine("========================================");

            // 1. Initial configuration
            int serverPort = 9001; // Different from AuthServer (9000)
            string redisConn = Environment.GetEnvironmentVariable("REDIS_CONN") ?? "localhost";
            string mysqlHost = Environment.GetEnvironmentVariable("MYSQL_HOST") ?? "127.0.0.1";
            int mysqlPort = int.TryParse(Environment.GetEnvironmentVariable("MYSQL_PORT"), out var port) ? port : 3306;
            string mysqlUser = Environment.GetEnvironmentVariable("MYSQL_USER") ?? "root";
            string mysqlPass = Environment.GetEnvironmentVariable("MYSQL_PASS") ?? "tjdgus81!!";
            string mysqlDb = Environment.GetEnvironmentVariable("MYSQL_DB") ?? "game_db"; // Changed to game_db

            // 2. Initialize Managers
            var redisManager = new RedisManager(redisConn);
            var dbManager = new MySqlManager(mysqlHost, mysqlPort, mysqlUser, mysqlPass, mysqlDb);
            var channelManager = new GameServer.Core.ChannelManager(dbManager);

            // 3. Start SAEA Listener
            var cts = new CancellationTokenSource();
            
            var listener = new Listener(serverPort, 1000, 1024 * 4); // 1000 max connections, 4KB buffer
            listener.OnClientConnected = (session) => 
            {
                // Hook the newly connected session to a new handler
                var handler = new ClientHandler(session, redisManager, channelManager, dbManager);
            };
            
            listener.Start();

            // Catch Ctrl+C to shut down gracefully
            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("Shutting down server...");
                e.Cancel = true;
                cts.Cancel();
            };

            // Main loop wait
            try
            {
                await Task.Delay(-1, cts.Token);
            }
            catch (TaskCanceledException)
            {
            }
            
            Console.WriteLine("GameServer terminated gracefully.");
        }
    }
}
