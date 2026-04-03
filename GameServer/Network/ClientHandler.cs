using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GameServer.Cache;
using GameServer.Core;
using GameServer.Database;
using GameServer.Protos;
using Google.Protobuf;

namespace GameServer.Network
{
    public class ClientHandler
    {
        private readonly Session _session;
        private readonly RedisManager _redisManager;
        private readonly ChannelManager _channelManager;
        private readonly MySqlManager _dbManager;

        // Player state
        public Player? MyPlayer { get; private set; }
        public Channel? CurrentChannel { get; private set; }
        public bool IsAuthenticated { get; private set; } = false;
        public int AccountId { get; private set; } = 0; // The verified auth_db user ID

        public ClientHandler(Session session, RedisManager redisManager, ChannelManager channelManager, MySqlManager dbManager)
        {
            _session = session;
            _redisManager = redisManager;
            _channelManager = channelManager;
            _dbManager = dbManager;

            _session.OnPacketReceived = (packet, sess) => { _ = OnPacketReceivedAsync(packet); };
            _session.OnDisconnected = (sess) => { _ = OnDisconnectedAsync(); };
        }

        private async Task OnPacketReceivedAsync(GamePacket packet)
        {
            try
            {
                // If not authenticated, only allow EnterGameReq
                if (!IsAuthenticated)
                {
                    if (packet.PayloadCase == GamePacket.PayloadOneofCase.EnterGameReq)
                    {
                        await HandleEnterGameAsync(packet.EnterGameReq);
                    }
                    else
                    {
                        Console.WriteLine("[ClientHandler] Client sent non-auth packet before authentication. Disconnecting.");
                        _session.Disconnect();
                    }
                    return;
                }

                // Dispatch authenticated packets
                switch (packet.PayloadCase)
                {
                    case GamePacket.PayloadOneofCase.MoveReq:
                        await HandleMoveAsync(packet.MoveReq);
                        break;
                    case GamePacket.PayloadOneofCase.ChangeChannelReq:
                        await HandleChangeChannelAsync(packet.ChangeChannelReq);
                        break;
                    case GamePacket.PayloadOneofCase.ChatReq:
                        await HandleChatAsync(packet.ChatReq);
                        break;
                    case GamePacket.PayloadOneofCase.Ping:
                        await HandlePingAsync(packet.Ping);
                        break;
                    case GamePacket.PayloadOneofCase.GetCharacterListReq:
                        await HandleGetCharacterListAsync();
                        break;
                    case GamePacket.PayloadOneofCase.CreateCharacterReq:
                        await HandleCreateCharacterAsync(packet.CreateCharacterReq);
                        break;
                    case GamePacket.PayloadOneofCase.SelectCharacterReq:
                        await HandleSelectCharacterAsync(packet.SelectCharacterReq);
                        break;
                    case GamePacket.PayloadOneofCase.AttackReq:
                        await HandleAttackAsync(packet.AttackReq);
                        break;
                    default:
                        Console.WriteLine($"[{MyPlayer?.Username}] Unknown packet: {packet.PayloadCase}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClientHandler] Process Packet Error: {ex.Message}");
            }
        }

        private async Task OnDisconnectedAsync()
        {
            try
            {
                if (MyPlayer != null)
                {
                    // Save to database on disconnect
                    await _dbManager.SaveCharacterAsync(MyPlayer.Username, MyPlayer.Hp, MyPlayer.Level, MyPlayer.Exp);

                    if (CurrentChannel != null)
                    {
                        await CurrentChannel.LeaveAsync(MyPlayer);
                    }
                }
                Console.WriteLine($"[ClientHandler] {(MyPlayer == null ? "Client" : MyPlayer.Username)} session ended.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClientHandler] Disconnect Handle Error: {ex.Message}");
            }
        }

        public async Task SendPacketAsync(GamePacket packet)
        {
            // Session.Send is synchronous wrap on sockets currently, but we keep async signature for interface
            _session.Send(packet);
            await Task.CompletedTask;
        }

        private async Task HandleEnterGameAsync(C2S_EnterGameReq req)
        {
            Console.WriteLine($"[ClientHandler] Verifying token: {req.Token}");

            var sessionData = await _redisManager.GetSessionByTokenAsync(req.Token);
            var res = new S2C_EnterGameRes();

            if (sessionData != null)
            {
                AccountId = sessionData.Value.userId;
                IsAuthenticated = true;

                res.Success = true;
                res.Message = "토큰 검증 성공! 캐릭터를 선택해 주세요.";

                var outPacket = new GamePacket { EnterGameRes = res };
                await SendPacketAsync(outPacket);
                
                Console.WriteLine($"[ClientHandler] Account {AccountId} ({sessionData.Value.username}) authenticated.");
            }
            else
            {
                res.Success = false;
                res.Message = "유효하지 않거나 만료된 토큰입니다.";
                Console.WriteLine($"[ClientHandler] Token validation failed.");

                var outPacket = new GamePacket { EnterGameRes = res };
                await SendPacketAsync(outPacket);
                _session.Disconnect();
            }
        }

        private async Task HandleGetCharacterListAsync()
        {
            var characters = await _dbManager.GetCharactersAsync(AccountId);
            var res = new S2C_GetCharacterListRes();
            
            foreach (var c in characters)
            {
                res.Characters.Add(new CharacterInfo
                {
                    Name = c.name,
                    Hp = c.hp,
                    Level = c.level
                });
            }

            var packet = new GamePacket { GetCharacterListRes = res };
            await SendPacketAsync(packet);
        }

        private async Task HandleCreateCharacterAsync(C2S_CreateCharacterReq req)
        {
            var (success, msg) = await _dbManager.CreateCharacterAsync(AccountId, req.Name);
            var res = new S2C_CreateCharacterRes { Success = success, Message = msg };
            
            var packet = new GamePacket { CreateCharacterRes = res };
            await SendPacketAsync(packet);
        }

        private async Task HandleSelectCharacterAsync(C2S_SelectCharacterReq req)
        {
            var res = new S2C_SelectCharacterRes();

            // Verify the character belongs to this user (for security, maybe check GetCharactersAsync list)
            var stats = await _dbManager.LoadCharacterStatsAsync(req.Name);
            
            // Assume success if no exception (Need robust checking in production)
            int playerId = Math.Abs(req.Name.GetHashCode()); // Dummy unique ID

            MyPlayer = new Player(playerId, req.Name, this)
            {
                Hp = stats.hp,
                Level = stats.level,
                Exp = stats.exp
            };

            CurrentChannel = _channelManager.GetDefaultChannel();

            res.Success = true;
            res.Message = "게임에 접속했습니다.";
            res.MyPlayerId = playerId;

            var outPacket = new GamePacket { SelectCharacterRes = res };
            await SendPacketAsync(outPacket);

            // Join the channel
            await CurrentChannel.JoinAsync(MyPlayer);
            Console.WriteLine($"[ClientHandler] Player {req.Name} entered GameServer.");
        }

        private async Task HandleMoveAsync(C2S_MoveReq req)
        {
            if (MyPlayer == null || CurrentChannel == null) return;

            // Update local state
            MyPlayer.X = req.X;
            MyPlayer.Y = req.Y;
            MyPlayer.Z = req.Z;

            // Broadcast to others in the same channel
            var syncPacket = new GamePacket
            {
                MoveSync = new S2C_MoveSync
                {
                    PlayerId = MyPlayer.Id,
                    X = req.X,
                    Y = req.Y,
                    Z = req.Z
                }
            };
            
            await CurrentChannel.BroadcastAsync(syncPacket, excludePlayerId: MyPlayer.Id);
            // Console log to debug
            Console.WriteLine($"[{MyPlayer.Username}] Moved to ({req.X}, {req.Y}, {req.Z})");
        }

        private async Task HandleChangeChannelAsync(C2S_ChangeChannelReq req)
        {
            if (MyPlayer == null || CurrentChannel == null) return;

            Console.WriteLine($"[{MyPlayer.Username}] Suggested channel change: {CurrentChannel.ChannelId} -> {req.ChannelId}");

            // Already in this channel?
            if (CurrentChannel.ChannelId == req.ChannelId)
            {
                var failRes = new GamePacket { ChangeChannelRes = new S2C_ChangeChannelRes { Success = false, Message = "이미 해당 채널에 있습니다.", ChannelId = CurrentChannel.ChannelId } };
                await SendPacketAsync(failRes);
                return;
            }

            // Leave current channel
            await CurrentChannel.LeaveAsync(MyPlayer);

            // Join new channel
            var newChannel = _channelManager.GetChannel(req.ChannelId);
            await newChannel.JoinAsync(MyPlayer);
            CurrentChannel = newChannel;

            // Send success to client
            var successRes = new GamePacket { ChangeChannelRes = new S2C_ChangeChannelRes { Success = true, Message = $"{req.ChannelId}번 채널로 이동했습니다.", ChannelId = newChannel.ChannelId } };
            await SendPacketAsync(successRes);
        }

        private async Task HandleChatAsync(C2S_ChatReq req)
        {
            if (MyPlayer == null || CurrentChannel == null) return;
            
            var chatRes = new GamePacket
            {
                ChatRes = new S2C_ChatRes
                {
                    SenderId = MyPlayer.Id,
                    SenderUsername = MyPlayer.Username,
                    Message = req.Message,
                    IsGlobal = req.IsGlobal
                }
            };
            
            if (req.IsGlobal)
            {
                Console.WriteLine($"[GLOBAL CHAT] {MyPlayer.Username}: {req.Message}");
                await _channelManager.BroadcastToAllChannelsAsync(chatRes);
            }
            else
            {
                Console.WriteLine($"[CH {CurrentChannel.ChannelId} CHAT] {MyPlayer.Username}: {req.Message}");
                await CurrentChannel.BroadcastAsync(chatRes); // Use BroadcastAsync directly, but it should also send to the sender.
                // Wait, CurrentChannel.BroadcastAsync normally excludes by ID, but we didn't provide excludePlayerId here,
                // so it defaults to -1, which means everyone in the channel, including sender, gets it. That's good!
            }
        }

        private async Task HandlePingAsync(C2S_Ping req)
        {
            var pongPacket = new GamePacket
            {
                Pong = new S2C_Pong { Timestamp = req.Timestamp }
            };
            await SendPacketAsync(pongPacket);
        }

        private async Task HandleAttackAsync(C2S_AttackReq req)
        {
            if (MyPlayer == null || CurrentChannel == null) return;

            if (req.IsMonster)
            {
                if (CurrentChannel._monsters.TryGetValue(req.TargetId, out var monster))
                {
                    if (monster.IsDead) return;

                    // Distance Check
                    float dx = MyPlayer.X - monster.X;
                    float dy = MyPlayer.Y - monster.Y;
                    float dz = MyPlayer.Z - monster.Z;
                    float distance = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
                    
                    float attackRange = 5.0f; // Attack Range

                    if (distance > attackRange)
                    {
                        Console.WriteLine($"[{MyPlayer.Username}] Attack failed: Target {monster.Id} is too far ({distance:F1} > {attackRange}).");
                        return; // Ignore if too far
                    }

                    // Deal Damage
                    int damage = new Random().Next(10, 25); // Random damage between 10 and 24
                    monster.TakeDamage(damage, MyPlayer.Id);

                    Console.WriteLine($"[{MyPlayer.Username}] Attacked Monster {monster.Id} for {damage} dmg. (HP: {monster.Hp}/{monster.MaxHp})");

                    var attackSync = new GamePacket
                    {
                        AttackSync = new S2C_AttackSync
                        {
                            AttackerId = MyPlayer.Id,
                            IsAttackerMonster = false,
                            TargetId = monster.Id,
                            IsTargetMonster = true,
                            Damage = damage,
                            TargetHp = monster.Hp,
                            IsDead = monster.IsDead
                        }
                    };

                    await CurrentChannel.BroadcastAsync(attackSync);

                    if (monster.IsDead)
                    {
                        Console.WriteLine($"[Channel {CurrentChannel.ChannelId}] Monster {monster.Id} has been slain by {MyPlayer.Username}.");
                        // We could remove it from dictionary, or keep it for respawns.
                        // Let's remove and send despawn for now.
                        CurrentChannel._monsters.TryRemove(monster.Id, out _);
                        
                        var despawnPacket = new GamePacket
                        {
                            DespawnMonster = new S2C_DespawnMonster { MonsterId = monster.Id }
                        };
                        await CurrentChannel.BroadcastAsync(despawnPacket);

                        // Give EXP to player
                        MyPlayer.Exp += (monster.MonsterType == 1) ? 50 : 10;
                    }
                }
            }
            else
            {
                // PvP not implemented for now
                Console.WriteLine($"[{MyPlayer.Username}] tried to attack player {req.TargetId}, but PvP is disabled.");
            }
        }
    }
}
