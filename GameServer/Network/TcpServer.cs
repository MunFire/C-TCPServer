using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GameServer.Cache;
using GameServer.Database;

namespace GameServer.Network
{
    public class TcpServer
    {
        private readonly int _port;
        private readonly TcpListener _listener;
        private readonly RedisManager _redisManager;
        private readonly GameServer.Core.ChannelManager _channelManager;
        private readonly MySqlManager _dbManager;

        public TcpServer(int port, RedisManager redisManager, GameServer.Core.ChannelManager channelManager, MySqlManager dbManager)
        {
            _port = port;
            _redisManager = redisManager;
            _channelManager = channelManager;
            _dbManager = dbManager;
            _listener = new TcpListener(IPAddress.Any, _port);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _listener.Start();
            Console.WriteLine($"[GameServer] Started on 0.0.0.0:{_port}");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                    Console.WriteLine($"[GameServer] Client connected: {client.Client.RemoteEndPoint}");
                    
                    var handler = new ClientHandler(client, _redisManager, _channelManager, _dbManager);
                    _ = handler.ProcessAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[GameServer] Server shutting down...");
            }
            finally
            {
                _listener.Stop();
            }
        }
    }
}
