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

            // 3. Start TCP Server
            var cts = new CancellationTokenSource();
            var tcpServer = new TcpServer(serverPort, redisManager, channelManager, dbManager);

            // Catch Ctrl+C to shut down gracefully
            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("Shutting down server...");
                e.Cancel = true;
                cts.Cancel();
            };

            await tcpServer.StartAsync(cts.Token);
            Console.WriteLine("GameServer terminated gracefully.");
        }
    }
}
