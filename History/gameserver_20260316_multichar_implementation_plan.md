# Database Separation & Multi-Character System Plan

This plan details the changes necessary to separate the account database (`auth_db`) from the game data database (`game_db`), and implement a multi-character system where a single account can create, select, and play multiple characters.

## Proposed Changes

### Database Layer
#### [MODIFY] `AuthServer/Database/MySqlManager.cs`
- Upon user creation (`CREATE TABLE IF NOT EXISTS users`), ensure `id` is an `INT AUTO_INCREMENT PRIMARY KEY`.
- `VerifyUserAsync` should be modified to return both the boolean success and the `userId` (`int`) instead of just a message.

#### [MODIFY] `AuthServer/Network/ClientHandler.cs` (AuthServer)
- When issuing the Session Token via Redis, store the `userId` in Redis instead of just the `username`.

#### [MODIFY] `GameServer/Program.cs`
- Currently connects to `MYSQL_DB = auth_db`. This will be changed to `game_db`.
- (The GameServer `MySqlManager` will connect to `game_db` only, while relying on the `AuthServer` to have handled account auth).

#### [MODIFY] `GameServer/Database/MySqlManager.cs`
- Modify the `characters` table to utilize the `user_id`:
  ```sql
  CREATE TABLE IF NOT EXISTS characters (
      character_name VARCHAR(50) PRIMARY KEY,
      user_id INT NOT NULL,
      hp INT NOT NULL DEFAULT 100,
      level INT NOT NULL DEFAULT 1,
      exp INT NOT NULL DEFAULT 0,
      INDEX (user_id)
  )
  ```
- Add the following methods:
  - `GetCharactersAsync(int userId)`: Returns a list of all characters belonging to the user.
  - `CreateCharacterAsync(int userId, string characterName)`: Inserts a new character if the name doesn't exist.

### Protocol definitions
#### [MODIFY] `Protos/gameserver.proto`
- Add new `PayloadOneofCase` messages:
  - `C2S_GetCharacterListReq` / `S2C_GetCharacterListRes`
  - `C2S_CreateCharacterReq` / `S2C_CreateCharacterRes`
  - `C2S_SelectCharacterReq` / `S2C_SelectCharacterRes` (This will essentially replace the initial blind entry of `EnterGameReq`).

### Game Network & Logic
#### [MODIFY] `GameServer/Network/ClientHandler.cs` (GameServer)
- Change `HandleEnterGameAsync` sequence:
  1. Client sends Token via `EnterGameReq`.
  2. Server validates Token, retrieves `userId` from Redis.
  3. Respond with `EnterGameRes` (Success). Server is now authenticated but NOT in a channel.
- Add handlers for the new packets:
  - `HandleGetCharacterListAsync`: Queries DB for `userId` and returns names/stats.
  - `HandleCreateCharacterAsync`: Attempts to create character. Returns success/fail.
  - `HandleSelectCharacterAsync`: Loads the specific `character_name`. Assigns to `Player` object. Spawns player into `ChannelManager.GetDefaultChannel()`.

### Client UI & Logic
#### [MODIFY] `WinFormsClient/Form1.cs` & `WinFormsClient/Form1.Designer.cs`
- After connecting and sending Token, instead of showing the Game/Chat UI immediately, show a **Character Selection UI**.
  - A ListBox/DataGrid showing existing characters.
  - A "Create Character" TextBox and Button.
  - A "Start Game" button for the selected character.
- The Game/Chat UI (health, positions, chat box) will remain hidden until `S2C_SelectCharacterRes` confirms successful entry into the world.

## Verification Plan
- Build all three projects (AuthServer, GameServer, WinFormsClient).
- Create a new account.
- Log in -> See empty character list -> Create 'Warrior' -> See 'Warrior' in list.
- Select 'Warrior' -> Enter game -> Confirm chat/heartbeat works.
- Re-log -> See 'Warrior' and stats -> Select and enter.
