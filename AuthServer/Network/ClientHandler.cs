using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AuthServer.Protos;
using AuthServer.Database;
using AuthServer.Cache;
using Google.Protobuf;

namespace AuthServer.Network
{
    public class ClientHandler
    {
        private readonly Session _session;
        private readonly MySqlManager _dbManager;
        private readonly RedisManager _redisManager;

        public ClientHandler(Session session, MySqlManager dbManager, RedisManager redisManager)
        {
            _session = session;
            _dbManager = dbManager;
            _redisManager = redisManager;

            _session.OnPacketReceived = (packet, sess) => { _ = OnPacketReceivedAsync(packet); };
            _session.OnDisconnected = (sess) => { Console.WriteLine("[ClientHandler] Session ended."); };
        }

        private async Task OnPacketReceivedAsync(Packet packet)
        {
            try
            {
                switch (packet.PayloadCase)
                {
                    case Packet.PayloadOneofCase.LoginReq:
                        await HandleLoginAsync(packet.LoginReq);
                        break;
                    case Packet.PayloadOneofCase.RegisterReq:
                        await HandleRegisterAsync(packet.RegisterReq);
                        break;
                    default:
                        Console.WriteLine($"[ClientHandler] Unknown packet type received: {packet.PayloadCase}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClientHandler] Process Packet Error: {ex.Message}");
            }
        }

        private async Task HandleLoginAsync(LoginRequest request)
        {
            Console.WriteLine($"[ClientHandler] Login request for user: {request.Username}");

            var (isValidUser, msg, userId) = await _dbManager.VerifyUserAsync(request.Username, request.Password);
            var response = new LoginResponse();

            if (isValidUser)
            {
                string token = Guid.NewGuid().ToString("N");
                
                try 
                {
                    string sessionData = $"{userId}:{request.Username}";
                    await _redisManager.SaveSessionAsync(sessionData, token, TimeSpan.FromHours(1));
                } 
                catch (Exception ex) 
                {
                    Console.WriteLine($"[ClientHandler] Redis is offline, skipping session save: {ex.Message}");
                }

                response.Success = true;
                response.Message = msg;
                response.Token = token;
                Console.WriteLine($"[ClientHandler] User {request.Username} logged in.");
            }
            else
            {
                response.Success = false;
                response.Message = msg;
                response.Token = string.Empty;
                Console.WriteLine($"[ClientHandler] User {request.Username} failed to login: {msg}");
            }

            var packet = new Packet { LoginRes = response };
            _session.Send(packet);
        }

        private async Task HandleRegisterAsync(RegisterRequest request)
        {
            Console.WriteLine($"[ClientHandler] Registration request for user: {request.Username}");

            var (success, message) = await _dbManager.CreateUserAsync(request.Username, request.Password);
            var response = new RegisterResponse
            {
                Success = success,
                Message = message
            };

            var packet = new Packet { RegisterRes = response };
            _session.Send(packet);
            Console.WriteLine($"[ClientHandler] Registration result for {request.Username}: {message}");
        }
    }
}
