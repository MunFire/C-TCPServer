# Add Chat and Broadcast Message Features

We will implement a feature to allow users to send chat messages to other users in the same channel, as well as a feature to broadcast a message to all channels.

## Proposed Changes

### Protos
#### [MODIFY] [gameserver.proto](file:///c:/MSH_Server/GameServer/Protos/gameserver.proto)
- Add new oneof payloads for chat request and response in `GamePacket`:
  ```protobuf
  C2S_ChatReq chatReq = 9;
  S2C_ChatRes chatRes = 10;
  ```
- Define the new message structures for chatting:
  ```protobuf
  // Client -> Server: "Send a chat message"
  message C2S_ChatReq {
    string message = 1;
    bool isGlobal = 2; // true if broadcasting to all channels, false for current channel
  }

  // Server -> Client: "Receive a chat message"
  message S2C_ChatRes {
    int32 senderId = 1;
    string senderUsername = 2;
    string message = 3;
    bool isGlobal = 4;
  }
  ```

---

### Core Components
#### [MODIFY] [ChannelManager.cs](file:///c:/MSH_Server/GameServer/Core/ChannelManager.cs)
- Add missing `using System.Linq;`, `using System.Threading.Tasks;`, and `using GameServer.Protos;`.
- Add a helper method `BroadcastToAllChannelsAsync` that iterates through all channels and broadcasts the message.
  ```csharp
  public async Task BroadcastToAllChannelsAsync(GamePacket packet)
  {
      var tasks = _channels.Values.Select(c => c.BroadcastAsync(packet));
      await Task.WhenAll(tasks);
  }
  ```

---

### Network Handlers
#### [MODIFY] [ClientHandler.cs](file:///c:/MSH_Server/GameServer/Network/ClientHandler.cs)
- In `ProcessAsync`, add a case for `GamePacket.PayloadOneofCase.ChatReq` to handle chat messages.
- Add `HandleChatAsync(C2S_ChatReq req)` method:
  - If `req.IsGlobal` is false, use `CurrentChannel.BroadcastAsync` to send to the current channel.
  - If `req.IsGlobal` is true, use `_channelManager.BroadcastToAllChannelsAsync` to broadcast to all channels.

## Verification Plan

### Automated Tests
1. Verify the project builds successfully with `dotnet build` in `c:\MSH_Server\GameServer`. This checks the generation of C# types from the updated proto file, and confirms no compile errors were introduced in the C# code.

### Manual Verification
1. As the server changes require the client to also update its proto and send the new `C2S_ChatReq`, the user should manually test this.
2. The user will run the game client, connect two players to the same channel, and send a regular chat message to verify they both receive it.
3. The user will connect a third player to a different channel, and one player will send a broadcast message (isGlobal = true) to verify players in all channels receive the broadcast.
