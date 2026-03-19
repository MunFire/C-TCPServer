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
        private readonly TcpClient _client;
        private readonly MySqlManager _dbManager;
        private readonly RedisManager _redisManager;

        public ClientHandler(TcpClient client, MySqlManager dbManager, RedisManager redisManager)
        {
            _client = client;
            _dbManager = dbManager;
            _redisManager = redisManager;
        }

        public async Task ProcessAsync(CancellationToken cancellationToken)
        {
            using (_client)
            using (var stream = _client.GetStream())
            {
                Console.WriteLine($"[ClientHandler] Session started.");
                
                try
                {
                    // Continuous loop to keep the connection alive
                    while (_client.Connected && !cancellationToken.IsCancellationRequested)
                    {
                        var packet = Packet.Parser.ParseDelimitedFrom(stream);
                        if (packet == null)
                        {
                            // Expected when client disconnects gracefully
                            break;
                        }

                        // Dispatch based on packet type
                        switch (packet.PayloadCase)
                        {
                            case Packet.PayloadOneofCase.LoginReq:
                                await HandleLoginAsync(stream, packet.LoginReq);
                                break;
                            case Packet.PayloadOneofCase.RegisterReq:
                                await HandleRegisterAsync(stream, packet.RegisterReq);
                                break;
                            default:
                                Console.WriteLine($"[ClientHandler] Unknown packet type received: {packet.PayloadCase}");
                                break;
                        }
                    }
                }
                catch (InvalidProtocolBufferException)
                {
                    Console.WriteLine("[ClientHandler] Invalid Protobuf message received (Disconnecting).");
                }
                catch (IOException)
                {
                    Console.WriteLine("[ClientHandler] Connection closed by client.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ClientHandler] Unexpected error: {ex.Message}");
                }
                finally
                {
                    Console.WriteLine("[ClientHandler] Session ended.");
                }
            }
        }

        private async Task HandleLoginAsync(NetworkStream stream, LoginRequest request)
        {
            Console.WriteLine($"[ClientHandler] Login request for user: {request.Username}");

            var (isValidUser, msg, userId) = await _dbManager.VerifyUserAsync(request.Username, request.Password);
            var response = new LoginResponse();

            if (isValidUser)
            {
                string token = Guid.NewGuid().ToString("N");
                
                try 
                {
                    // Store both Username and UserId separated by colon, or just store a JSON. 
                    // Let's store "userId:username" to make it easy to parse in GameServer.
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

            // Wrap in Packet and send
            var packet = new Packet { LoginRes = response };
            packet.WriteDelimitedTo(stream);
            await stream.FlushAsync();
        }

        private async Task HandleRegisterAsync(NetworkStream stream, RegisterRequest request)
        {
            Console.WriteLine($"[ClientHandler] Registration request for user: {request.Username}");

            var (success, message) = await _dbManager.CreateUserAsync(request.Username, request.Password);
            var response = new RegisterResponse
            {
                Success = success,
                Message = message
            };

            // Wrap in Packet and send
            var packet = new Packet { RegisterRes = response };
            packet.WriteDelimitedTo(stream);
            await stream.FlushAsync();
            Console.WriteLine($"[ClientHandler] Registration result for {request.Username}: {message}");
        }
    }
}
