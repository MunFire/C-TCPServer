# Player Stats & MySQL Sync Feature

We will implement a system that loads the player's character data (HP, Level, EXP) from a MySQL database upon login, and periodically saves this data back to MySQL every 10 minutes to prevent data loss.

## Proposed Changes

### Dependencies
#### [MODIFY] `GameServer.csproj`
- Add `MySqlConnector` package to enable MySQL connectivity.

### Database Layer
#### [NEW] [MySqlManager.cs](file:///c:/MSH_Server/GameServer/Database/MySqlManager.cs)
- Create a `MySqlManager` analogous to the AuthServer's DB manager.
- **Methods**:
  - `LoadCharacterAsync(string username)`: Selects `hp, level, exp` from the `characters` table. If the row doesn't exist, it creates a new one with default stats (e.g., HP 100, Lv 1).
  - `SaveCharacterAsync(string username, int hp, int level, int exp)`: Uses an UPSERT (`INSERT ... ON DUPLICATE KEY UPDATE`) to save the latest stats.

### Core Components
#### [MODIFY] [Player.cs](file:///c:/MSH_Server/GameServer/Core/Player.cs)
- Add new properties:
  ```csharp
  public int Hp { get; set; }
  public int Level { get; set; }
  public int Exp { get; set; }
  ```

#### [MODIFY] [ChannelManager.cs](file:///c:/MSH_Server/GameServer/Core/ChannelManager.cs)
- Add dependency injection for `MySqlManager`.
- Initialize a `System.Threading.Timer` that triggers every 10 minutes.
- When triggered, it will iterate through all `Channel` instances and their `Player` instances, calling `SaveCharacterAsync` for each player to back up their data synchronously or concurrently.

### Network & Initialization
#### [MODIFY] [Program.cs](file:///c:/MSH_Server/GameServer/Program.cs) & [TcpServer.cs](file:///c:/MSH_Server/GameServer/Network/TcpServer.cs)
- Initialize `MySqlManager` with environment variables (`MYSQL_HOST`, etc. defaulting to DB variables used by AuthServer).
- Pass it down to `ChannelManager` and `TcpServer`.

#### [MODIFY] [ClientHandler.cs](file:///c:/MSH_Server/GameServer/Network/ClientHandler.cs)
- Pass `MySqlManager` inside.
- During `HandleEnterGameAsync`, after authenticating the token, call `_dbManager.LoadCharacterAsync(username)` and assign the loaded stats to the newly created `Player` object.
- On `Disconnect` / `LeaveAsync` (in `finally` block or `OnDisconnect`), do a final save: `_dbManager.SaveCharacterAsync(...)` so no data is lost upon quitting before the 10-minute timer.

## Verification
1. Add `MySqlConnector` and `dotnet build`.
2. Manually test the game client: Connect, observe the console for `LoadCharacter` logic, play for a minute, disconnect, and check if final stats were saved.
3. Keep the server running and ensure the 10-minute automated save logs its operation without crashing. Note: For testing purposes, we can temporarily set it to 1 minute, verify, and then change it to 10 minutes.
