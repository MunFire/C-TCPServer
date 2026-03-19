using MySqlConnector;
using System.Threading.Tasks;
using System;

namespace AuthServer.Database
{
    public class MySqlManager
    {
        private readonly string _connectionString;

        public MySqlManager(string host, int port, string user, string password, string database)
        {
            _connectionString = $"Server={host};Port={port};User ID={user};Password={password};Database={database};";
        }

        // Dummy/basic user verification using plain text (in production, use hashed passwords)
        public async Task<(bool success, string message, int userId)> VerifyUserAsync(string username, string password)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT id, password FROM users WHERE username = @User";
                command.Parameters.AddWithValue("@User", username);

                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    int userId = reader.GetInt32(0);
                    // User exists, check password
                    string dbPassword = reader.GetString(1);
                    if (dbPassword == password)
                    {
                        return (true, "로그인에 성공했습니다.", userId); // Login success
                    }
                    else
                    {
                        return (false, "비밀번호가 일치하지 않습니다.", 0); // Wrong password
                    }
                }
                else
                {
                    return (false, "존재하지 않는 아이디입니다.", 0); // User not found
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MySQL Error] {ex.ToString()}");
                return (false, "데이터베이스 오류가 발생했습니다.", 0);
            }
        }

        public async Task<(bool success, string message)> CreateUserAsync(string username, string password)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // Create table with auto-increment ID if it doesn't exist
                using var createTableCommand = connection.CreateCommand();
                createTableCommand.CommandText = @"
                    CREATE TABLE IF NOT EXISTS users (
                        id INT AUTO_INCREMENT PRIMARY KEY,
                        username VARCHAR(50) UNIQUE NOT NULL,
                        password VARCHAR(255) NOT NULL
                    )";
                await createTableCommand.ExecuteNonQueryAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO users (username, password) VALUES (@User, @Pass)";
                command.Parameters.AddWithValue("@User", username);
                command.Parameters.AddWithValue("@Pass", password); // Should be hashed in real apps

                await command.ExecuteNonQueryAsync();
                return (true, "Registration successful");
            }
            catch (MySqlException ex)
            {
                // Duplicate username error code in MySQL is usually 1062
                if (ex.Number == 1062)
                {
                    return (false, "Username already exists");
                }
                Console.WriteLine($"[MySQL Error] {ex.ToString()}");
                return (false, "Database error during registration");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MySQL Error] {ex.ToString()}");
                return (false, "Unexpected registration error");
            }
        }
    }
}
