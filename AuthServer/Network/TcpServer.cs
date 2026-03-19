using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AuthServer.Database;
using AuthServer.Cache;

namespace AuthServer.Network
{
    public class TcpServer
    {
        private readonly TcpListener _listener;
        private readonly MySqlManager _dbManager;
        private readonly RedisManager _redisManager;
        private bool _isRunning;

        public TcpServer(string ipAddress, int port, MySqlManager dbManager, RedisManager redisManager)
        {
            _listener = new TcpListener(IPAddress.Parse(ipAddress), port);
            _dbManager = dbManager;
            _redisManager = redisManager;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _listener.Start();
            _isRunning = true;
            Console.WriteLine($"[TcpServer] Started on {_listener.LocalEndpoint}");

            try
            {
                while (_isRunning && !cancellationToken.IsCancellationRequested)
                {
                    // Accept TcpClient asynchronously
                    var tcpClient = await _listener.AcceptTcpClientAsync(cancellationToken);
                    Console.WriteLine($"[TcpServer] Client connected: {tcpClient.Client.RemoteEndPoint}");
                    
                    // Handle client in a separate Task (multi-threaded behavior)
                    _ = Task.Run(() => HandleClientAsync(tcpClient, cancellationToken), cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[TcpServer] Server stopped.");
            }
            finally
            {
                Stop();
            }
        }

        private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken cancellationToken)
        {
            try
            {
                var handler = new ClientHandler(tcpClient, _dbManager, _redisManager);
                await handler.ProcessAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TcpServer] Error handling client: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;
            _isRunning = false;
            _listener.Stop();
        }
    }
}
