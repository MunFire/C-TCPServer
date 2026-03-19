# Add Heartbeat (Ping/Pong) Feature

We will implement a heartbeat mechanism to ensure the connection between the client and the GameServer remains active. If the server does not receive a heartbeat within a specific timeframe, it will forcefully disconnect the client to save resources.

## Proposed Changes

### Protos
#### [MODIFY] [gameserver.proto](file:///c:/MSH_Server/GameServer/Protos/gameserver.proto)
- Add new oneof payloads for ping and pong in `GamePacket`:
  ```protobuf
  C2S_Ping ping = 11;
  S2C_Pong pong = 12;
  ```
- Define the new message structures for heartbeat:
  ```protobuf
  // Client -> Server: "Are you there?" (Sent periodically)
  message C2S_Ping {
    int64 timestamp = 1;
  }

  // Server -> Client: "Yes, I'm here."
  message S2C_Pong {
    int64 timestamp = 1;
  }
  ```
- After modification, we will copy this file to `WinFormsClient\Protos\gameserver.proto`.

---

### GameServer
#### [MODIFY] [ClientHandler.cs](file:///c:/MSH_Server/GameServer/Network/ClientHandler.cs)
- Add `private DateTime _lastHeartbeatTime;`.
- Initialize `_lastHeartbeatTime = DateTime.UtcNow;` in the constructor or upon `EnterGameReq`.
- In `ProcessAsync`, update `_lastHeartbeatTime` and handle `C2S_Ping`:
  - Whenever *any* packet is received, you could update `_lastHeartbeatTime = DateTime.UtcNow;` to indicate the client is alive.
  - Specifically, for `GamePacket.PayloadOneofCase.Ping`, reply immediately with `S2C_Pong` echoing the timestamp.
  
#### [MODIFY] [TcpServer.cs](file:///c:/MSH_Server/GameServer/Network/TcpServer.cs) Add Timeout Checker
- *Alternatively*, to keep things simple and within `ClientHandler`'s scope without a global manager yet, we can modify `ClientHandler.ProcessAsync` to have read timeouts. 
- A cleaner approach for now: In `ClientHandler.ProcessAsync`, use `_stream.ReadTimeout = 30000;` (30 seconds). If a timeout occurs, an `IOException` is thrown, which we already catch to disconnect the client. This natively works well with C# TCP streams.

---

### WinFormsClient
#### [MODIFY] [Form1.cs](file:///c:/MSH_Server/WinFormsClient/Form1.cs)
- Add a `System.Windows.Forms.Timer _heartbeatTimer;`.
- Initialize it in `SwitchToGameServer`: `Interval = 10000` (10 seconds), `Tick += SendHeartbeat;`.
- Implement `SendHeartbeat` method:
  - Create a `C2S_Ping` with `DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()`.
  - Send the packet.
- In `GameReceiveLoopAsync`, handle `S2C_Pong`:
  - Note the latency or simply log `[Heartbeat] Pong received`.
- In `OnFormClosing` and when disconnected, `_heartbeatTimer?.Stop()`.

## Verification Plan
1. `dotnet build` on both `GameServer` and `WinFormsClient`.
2. Save this plan to `History` as requested previously, for record-keeping.
