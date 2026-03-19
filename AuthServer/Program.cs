using System;
using System.Threading;
using System.Threading.Tasks;
using AuthServer.Database;
using AuthServer.Cache;
using AuthServer.Network;

namespace AuthServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("    AuthServer - TCP/Protobuf Server    ");
            Console.WriteLine("========================================");

            // 1. Configuration (In production, read from appsettings.json or Environment Variables)
            string mysqlHost = Environment.GetEnvironmentVariable("MYSQL_HOST") ?? "127.0.0.1";
            int mysqlPort = int.TryParse(Environment.GetEnvironmentVariable("MYSQL_PORT"), out var port) ? port : 3306;
            string mysqlUser = Environment.GetEnvironmentVariable("MYSQL_USER") ?? "root";
            string mysqlPass = Environment.GetEnvironmentVariable("MYSQL_PASS") ?? "tjdgus81!!";
            string mysqlDb = Environment.GetEnvironmentVariable("MYSQL_DB") ?? "auth_db";

            string redisConn = Environment.GetEnvironmentVariable("REDIS_CONN") ?? "localhost";

            string serverIp = Environment.GetEnvironmentVariable("SERVER_IP") ?? "0.0.0.0";
            int serverPort = int.TryParse(Environment.GetEnvironmentVariable("SERVER_PORT"), out var sPort) ? sPort : 9000;

            // 2. Initialize Managers
            var dbManager = new MySqlManager(mysqlHost, mysqlPort, mysqlUser, mysqlPass, mysqlDb);
            var redisManager = new RedisManager(redisConn);

            // 3. Initialize & Start TCP Server
            var tcpServer = new TcpServer(serverIp, serverPort, dbManager, redisManager);

            using var cts = new CancellationTokenSource();

            // Handle graceful shutdown on Ctrl+C
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true; 
                Console.WriteLine("\n[System] Shutdown requested, closing server...");
                cts.Cancel();
            };

            await tcpServer.StartAsync(cts.Token);
            
            Console.WriteLine("[System] Server cleanly exited.");
        }
    }
}
