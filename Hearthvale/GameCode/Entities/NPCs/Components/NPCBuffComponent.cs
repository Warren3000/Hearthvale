using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Hearthvale.GameCode.Entities.NPCs.Components
{
    /// <summary>
    /// Manages NPC buff system including timed attack buffs
    /// </summary>
    public class NpcBuffComponent
    {
        private readonly NPC _owner;
        private readonly List<AttackBuff> _attackBuffs = new();
        private bool _enrageApplied;
        private float _buffPulseTimer = 0f;

        public bool HasActiveTimedAttackBuff => _attackBuffs.Count > 0;
        public float BuffPulseTimer => _buffPulseTimer;

        private readonly struct AttackBuff
        {
            public readonly int Delta;
            public readonly float TimeLeft;

            public AttackBuff(int delta, float timeLeft)
            {
                Delta = delta;
                TimeLeft = timeLeft;
            }

            public AttackBuff Tick(float dt) => new AttackBuff(Delta, TimeLeft - dt);
            public bool Expired => TimeLeft <= 0f;
        }

        public NpcBuffComponent(NPC owner)
        {
            _owner = owner;
        }

        public void Update(float deltaTime)
        {
            // Update timed buffs
            if (_attackBuffs.Count > 0)
            {
                for (int i = _attackBuffs.Count - 1; i >= 0; i--)
                {
                    var next = _attackBuffs[i].Tick(deltaTime);
                    if (next.Expired)
                    {
                        _owner.AttackPower -= _attackBuffs[i].Delta;
                        _attackBuffs.RemoveAt(i);
                    }
                    else
                    {
                        _attackBuffs[i] = next;
                    }
                }
            }

            // Visual indicator: pulse while any timed attack buff is active
            if (HasActiveTimedAttackBuff)
            {
                _buffPulseTimer -= deltaTime;
                if (_buffPulseTimer <= 0f)
                {
                    _owner.Sprite?.Flash(Color.Gold, 0.12f);
                    _buffPulseTimer = 0.8f;
                }
            }
            else
            {
                _buffPulseTimer = 0f;
            }

            // Type-specific conditional buffs
            CheckConditionalBuffs();
        }

        private void CheckConditionalBuffs()
        {
            // Example: HeavyKnight enrages under 50% HP
            if (_owner.Class == NpcClass.HeavyKnight && !_enrageApplied &&
                _owner.Health <= (_owner.MaxHealth / 2))
            {
                _enrageApplied = true;
                ApplyAttackBuff(+2, 10f);
            }
        }

        public void ApplyAttackBuff(int flatDelta, float durationSeconds)
        {
            if (flatDelta == 0 || durationSeconds <= 0f) return;

            _attackBuffs.Add(new AttackBuff(flatDelta, durationSeconds));
            _owner.AttackPower += flatDelta;

            _owner.Sprite?.Flash(Color.Gold, 0.35f);
            _buffPulseTimer = 0.3f;
        }

        public void ConfigureTypeAttackBuffs()
        {
            switch (_owner.Class)
            {
                case NpcClass.Merchant:
                    // No combat buffs
                    break;

                case NpcClass.Knight:
                    // Flat permanent bonus
                    _owner.AttackPower += 2;
                    _owner.Sprite?.Flash(Color.Goldenrod, 0.25f);
                    break;

                case NpcClass.HeavyKnight:
                    _owner.AttackPower += 3;
                    _owner.AttackCooldown = MathF.Max(_owner.AttackCooldown, 2.0f);
                    _owner.Sprite?.Flash(Color.Goldenrod, 0.35f);
                    break;
            }
        }
    }
}