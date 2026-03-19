using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using AuthServer.Protos;
using Google.Protobuf;

namespace TestClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("    TestClient - AuthServer Connection    ");
            Console.WriteLine("========================================");

            string serverIp = "127.0.0.1";
            int port = 9000;

            try
            {
                using var client = new TcpClient();
                Console.WriteLine($"[TestClient] Connecting to AuthServer at {serverIp}:{port}...");
                await client.ConnectAsync(serverIp, port);
                Console.WriteLine("[TestClient] Connected successfully!");

                using var stream = client.GetStream();

                // 1. Create a LoginRequest
                var request = new LoginRequest
                {
                    Username = "testuser",
                    Password = "1234"
                };

                Console.WriteLine($"[TestClient] Sending LoginRequest for user: {request.Username}");

                // 2. Send the request (Delimited ensures exact length is prepended)
                request.WriteDelimitedTo(stream);
                await stream.FlushAsync();

                // 3. Receive the response
                var response = LoginResponse.Parser.ParseDelimitedFrom(stream);
                if (response != null)
                {
                    Console.WriteLine("[TestClient] Received Response:");
                    Console.WriteLine($"  Success: {response.Success}");
                    Console.WriteLine($"  Message: {response.Message}");
                    Console.WriteLine($"  Token  : {(string.IsNullOrEmpty(response.Token) ? "NULL" : response.Token)}");
                }
                else
                {
                    Console.WriteLine("[TestClient] Received null or empty response.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TestClient] Error: {ex.Message}");
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
