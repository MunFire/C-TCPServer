# GameServer Implementation Plan

This document outlines the architecture and step-by-step plan for building the `GameServer` which handles in-game logic, session verification via Redis, channel management, and movement synchronization.

## 1. Project Structure
We will create a new Console Application project named `GameServer` next to `AuthServer`.

```
C:\MSH_Server\
├── AuthServer\
├── WinFormsClient\
└── GameServer\            [NEW]
    ├── Protos\
    │   └── gameserver.proto     (Packet definitions for in-game)
    ├── Network\
    │   ├── TcpServer.cs
    │   └── ClientHandler.cs
    ├── Core\
    │   ├── ChannelManager.cs (Manages all channels)
    │   ├── Channel.cs        (Manages players in a specific channel)
    │   └── Player.cs         (Player state: session, coordinates)
    └── Cache\
        └── RedisManager.cs   (For verifying tokens from AuthServer)
```

## 2. Component Details

### Protobuf (`gameserver.proto`)
Instead of mixing with `auth.proto`, we will create a dedicated `gameserver.proto` for game-specific interactions.
- `GamePacket` (oneof wrapper)
- `C2S_EnterGameReq` (Contains the Token received from AuthServer)
- `S2C_EnterGameRes` (Success/Fail)
- `C2S_MoveReq` (X, Y, Z)
- `S2C_MoveSync` (PlayerID, X, Y, Z - Broadcast to others)
- `C2S_ChatReq` / `S2C_ChatNotify` (Optional for later)

### Authentication Flow (Token & Redis)
1. User logs into `AuthServer` and gets a Token.
2. User disconnects from AuthServer and connects to `GameServer` directly.
3. User sends `C2S_EnterGameReq` with the Token.
4. `GameServer` checks `RedisManager` to see if that Token exists and brings up the username.
5. If valid, the connection becomes an authenticated `Player` object.

### Channel System
- `ChannelManager` will hold a Dictionary or List of active `Channel` instances (e.g., Channel 1 ~ Channel 10).
- When a user authenticates, they are assigned to a Default Channel (e.g., Channel 1), or they can specify a channel.
- A `Channel` contains a list of `.ConnectedPlayers`.

### Movement Synchronization (Broadcast)
- When a `ClientHandler` receives `C2S_MoveReq(x,y,z)`, it updates its own `Player` transform.
- It then calls a method on its assigned `Channel`: `channel.BroadcastMove(player.Id, x, y, z)`.
## 3. Phase 6: Chat System & Feature Recommendations

### New Features (Planned)
1. **Channel Chat (`C2S_ChatReq` / `S2C_ChatNotify`)**
   - Players can send messages that are broadcasted only to other players in the same `Channel`.
2. **Global Shout (`C2S_GlobalChatReq` / `S2C_GlobalChatNotify`)**
   - Players can send a "Megaphone" message that is broadcasted to *all* channels via `ChannelManager`.
3. **Recommended Future Features:**
   - **Heartbeat (Ping/Pong):** To detect disconnected clients early ("좀비 세션" 정리).
   - **Player Stats & DB Sync:** Saving character data (HP, Level, EXP) to MySQL periodically.
   - **Entity System:** Adding NPCs or Monsters to the channel that the server controls and syncs.

## 4. Thread Safety & Deadlock Analysis Review

The current GameServer uses `async/await` with `TcpClient` and `ConcurrentDictionary`. Here is a brief analysis of thread safety:

### Current Strengths
- **`ConcurrentDictionary` in ChannelManager and Channel:** We are using `ConcurrentDictionary`, which is inherently thread-safe for basic operations like `TryAdd`, `TryRemove`, and `TryGetValue`. This prevents race conditions when multiple players join/leave exactly at the same millisecond.
- **Asynchronous I/O:** `await stream.FlushAsync()` inside `ClientHandler` uses non-blocking I/O. The threads are returned to the ThreadPool while waiting for network.

### Potential Deadlock/Concurrency Risks
1. **`Channel.BroadcastAsync` iteration:**
   - Currently, `BroadcastAsync` iterates over `_players.Values`. While `ConcurrentDictionary` allows safe iteration, the state of the dictionary might change *during* the iteration. The iteration itself is safe (won't throw `InvalidOperationException`), but the snapshot of players might be slightly stale. This is generally acceptable for a game.
2. **Task.WhenAll in Broadcast:**
   - `BroadcastAsync` uses `await Task.WhenAll(tasks)`. This waits for all sends to complete. If one client has a painfully slow connection or a blocked socket, it *could* theoretically delay the `Task.WhenAll` completion for the thread broadcasting the move.
   - **Mitigation:** We should eventually implement a fire-and-forget mechanism or queue-based sending for broadcasting, so one slow client doesn't slow down the movement updates for everyone else in the channel.
3. **Deadlocks:**
   - A classic deadlock occurs when Thread A locks Object 1 and waits for Object 2, while Thread B locks Object 2 and waits for Object 1.
   - We are currently **not using any explicit `lock (...)` statements** in the core game logic (we rely purely on `ConcurrentDictionary` and stateless async methods). Because there are no nested locks taking place, the risk of a traditional thread deadlock is **extremely low**.

### Conclusion on Threading
To make the server robust for a real MMO, we should add a **Send Queue** (e.g., `Channel<GamePacket>`) for each `Player`. Instead of the broadcaster awaiting `_stream.FlushAsync()` for every player, it just pushes the packet to everyone's queue. A dedicated background task for each player reads from their queue and sends to their socket. This completely isolates slow clients from affecting the server.

## 5. Review Request
Please review the Chat System features and the suggested Thread Safety improvements (Send Queue). Shall we proceed with adding the Chat System and refactoring the Network layer to use a Send Queue for better thread safety?
