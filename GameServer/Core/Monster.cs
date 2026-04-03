using System;
using System.Threading.Tasks;
using GameServer.Network;
using GameServer.Protos;

namespace GameServer.Core
{
    public class Monster
    {
        public int Id { get; private set; }
        public int MonsterType { get; private set; } // 0: Normal, 1: Boss
        
        // Transform
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        // Stats
        public int MaxHp { get; private set; }
        public int Hp { get; set; }
        public bool IsDead => Hp <= 0;

        // AI
        public bool IsAggressive { get; private set; }
        public int TargetPlayerId { get; set; } = -1;
        private DateTime _lastActionTime = DateTime.MinValue;
        private readonly float _attackRange = 4.0f;
        private readonly float _aggroRange = 15.0f;
        private readonly float _moveSpeed = 1.0f; // Units per tick

        public Monster(int id, int monsterType, float x, float y, float z, bool isAggressive)
        {
            Id = id;
            MonsterType = monsterType;
            X = x;
            Y = y;
            Z = z;
            IsAggressive = isAggressive;

            MaxHp = (monsterType == 1) ? 200 : 50;
            Hp = MaxHp;
        }

        public void TakeDamage(int damage, int attackerId)
        {
            if (IsDead) return;
            Hp -= damage;
            if (Hp < 0) Hp = 0;
            
            // Retaliate: Aggro to the attacker
            TargetPlayerId = attackerId;
        }

        public async Task UpdateAsync(Channel channel)
        {
            if (IsDead) return;

            // Cooldown check (act once every 1.5 seconds)
            if ((DateTime.Now - _lastActionTime).TotalSeconds < 1.5)
                return;

            // 1. If Aggressive and no target, find a nearby player
            if (IsAggressive && TargetPlayerId == -1)
            {
                Player nearestPlayer = null;
                float nearestDist = float.MaxValue;

                foreach (var p in channel.GetAllPlayers())
                {
                    if (p.IsDead) continue;
                    float dx = X - p.X;
                    float dy = Y - p.Y;
                    float dz = Z - p.Z;
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
                    
                    if (dist <= _aggroRange && dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearestPlayer = p;
                    }
                }

                if (nearestPlayer != null)
                {
                    TargetPlayerId = nearestPlayer.Id;
                    Console.WriteLine($"[AI] Monster {Id} saw Player {TargetPlayerId} and aggro'd!");
                }
            }

            // 2. If has target, act
            if (TargetPlayerId != -1)
            {
                var target = channel.GetPlayer(TargetPlayerId);
                
                // If target left channel or died
                if (target == null || target.IsDead)
                {
                    TargetPlayerId = -1;
                    return;
                }

                // Calculate distance
                float dx = target.X - X;
                float dy = target.Y - Y;
                float dz = target.Z - Z;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);

                if (distance > _aggroRange * 2) 
                {
                    // Target ran away too far
                    TargetPlayerId = -1;
                    return;
                }

                _lastActionTime = DateTime.Now;

                if (distance <= _attackRange)
                {
                    // Attack
                    int damage = (MonsterType == 1) ? new Random().Next(15, 35) : new Random().Next(5, 15);
                    target.TakeDamage(damage);

                    Console.WriteLine($"[AI] Monster {Id} attacks Player {target.Username} for {damage} dmg. (HP: {target.Hp}/{target.MaxHp})");

                    var attackPacket = new GamePacket
                    {
                        AttackSync = new S2C_AttackSync
                        {
                            AttackerId = Id,
                            IsAttackerMonster = true,
                            TargetId = target.Id,
                            IsTargetMonster = false,
                            Damage = damage,
                            TargetHp = target.Hp,
                            IsDead = target.IsDead
                        }
                    };
                    await channel.BroadcastAsync(attackPacket);
                }
                else
                {
                    // Move towards target
                    float ratio = _moveSpeed / distance;
                    X += dx * ratio;
                    Y += dy * ratio;
                    Z += dz * ratio;

                    var movePacket = new GamePacket
                    {
                        MonsterMoveSync = new S2C_MonsterMoveSync
                        {
                            MonsterId = Id,
                            X = X,
                            Y = Y,
                            Z = Z
                        }
                    };
                    await channel.BroadcastAsync(movePacket);
                }
            }
        }
    }
}
