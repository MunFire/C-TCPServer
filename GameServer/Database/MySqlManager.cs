using System;
using System.Threading.Tasks;
using MySqlConnector;

namespace GameServer.Database
{
    public class MySqlManager
    {
        private readonly string _connectionString;

        public MySqlManager(string host, int port, string user, string password, string database)
        {
            _connectionString = $"Server={host};Port={port};User ID={user};Password={password};Database={database};";
        }

        // Get a list of all characters belonging to a specific User ID
        public async Task<System.Collections.Generic.List<(string name, int hp, int level, int exp)>> GetCharactersAsync(int userId)
        {
            var characters = new System.Collections.Generic.List<(string, int, int, int)>();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                using var createTableCommand = connection.CreateCommand();
                createTableCommand.CommandText = @"
                    CREATE TABLE IF NOT EXISTS characters (
                        character_name VARCHAR(50) PRIMARY KEY,
                        user_id INT NOT NULL,
                        hp INT NOT NULL DEFAULT 100,
                        level INT NOT NULL DEFAULT 1,
                        exp INT NOT NULL DEFAULT 0,
                        INDEX idx_user_id (user_id)
                    )";
                await createTableCommand.ExecuteNonQueryAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT character_name, hp, level, exp FROM characters WHERE user_id = @UserId";
                command.Parameters.AddWithValue("@UserId", userId);

                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    characters.Add((reader.GetString(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3)));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MySQL Error - GetChars] {ex}");
            }
            return characters;
        }

        // Returns (success, message). Allows creating a specific character named by the user.
        public async Task<(bool success, string message)> CreateCharacterAsync(int userId, string characterName)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                using var insertCmd = connection.CreateCommand();
                insertCmd.CommandText = "INSERT INTO characters (character_name, user_id, hp, level, exp) VALUES (@Name, @Id, 100, 1, 0)";
                insertCmd.Parameters.AddWithValue("@Name", characterName);
                insertCmd.Parameters.AddWithValue("@Id", userId);
                await insertCmd.ExecuteNonQueryAsync();

                return (true, "캐릭터 생성 성공!");
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062) // Duplicate entry
                {
                    return (false, "이미 존재하는 이름입니다.");
                }
                Console.WriteLine($"[MySQL Error - CreateChar] {ex}");
                return (false, "데이터베이스 오류가 발생했습니다.");
            }
        }

        // Load specific character's stats by name
        public async Task<(int hp, int level, int exp)> LoadCharacterStatsAsync(string characterName)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT hp, level, exp FROM characters WHERE character_name = @Name";
                command.Parameters.AddWithValue("@Name", characterName);

                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    return (reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2));
                }
                return (100, 1, 0); // Should not happen if strictly selected from list
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MySQL Error - LoadStat] {ex}");
                return (100, 1, 0);
            }
        }

        public async Task SaveCharacterAsync(string characterName, int hp, int level, int exp)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE characters 
                    SET hp = @Hp, level = @Lv, exp = @Exp
                    WHERE character_name = @Name";

                command.Parameters.AddWithValue("@Name", characterName);
                command.Parameters.AddWithValue("@Hp", hp);
                command.Parameters.AddWithValue("@Lv", level);
                command.Parameters.AddWithValue("@Exp", exp);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MySQL Error - Save ID:{characterName}] {ex}");
            }
        }
    }
}
