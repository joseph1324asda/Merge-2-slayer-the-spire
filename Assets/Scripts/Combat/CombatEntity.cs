using System;

namespace Merge2.Combat
{
    public sealed class TurnStartContext
    {
        public CombatEntity Entity { get; private set; }
        public int TickIndex { get; private set; }

        public TurnStartContext(CombatEntity entity, int tickIndex)
        {
            Entity = entity;
            TickIndex = tickIndex;
        }
    }

    public sealed class AttackContext
    {
        public CombatEntity Attacker { get; private set; }
        public CombatEntity Target { get; private set; }
        public int TickIndex { get; private set; }

        public AttackContext(CombatEntity attacker, CombatEntity target, int tickIndex)
        {
            Attacker = attacker;
            Target = target;
            TickIndex = tickIndex;
        }
    }

    public sealed class DamageCalculationContext
    {
        public CombatEntity Attacker { get; private set; }
        public CombatEntity Target { get; private set; }
        public int RawAttack { get; private set; }
        public int TargetDefense { get; private set; }
        public int DamageAmount;
        public int TickIndex { get; private set; }

        public DamageCalculationContext(CombatEntity attacker, CombatEntity target, int rawAttack, int targetDefense, int damageAmount, int tickIndex)
        {
            Attacker = attacker;
            Target = target;
            RawAttack = rawAttack;
            TargetDefense = targetDefense;
            DamageAmount = damageAmount;
            TickIndex = tickIndex;
        }
    }

    public sealed class HitContext
    {
        public CombatEntity Attacker { get; private set; }
        public CombatEntity Target { get; private set; }
        public int MainDamageDealt { get; private set; }
        public int TickIndex { get; private set; }

        public HitContext(CombatEntity attacker, CombatEntity target, int mainDamageDealt, int tickIndex)
        {
            Attacker = attacker;
            Target = target;
            MainDamageDealt = mainDamageDealt;
            TickIndex = tickIndex;
        }
    }

    public sealed class ReceiveDamageContext
    {
        public CombatEntity Source { get; private set; }
        public CombatEntity Target { get; private set; }
        public int Amount { get; private set; }
        public string DamageType { get; private set; }
        public int TickIndex { get; private set; }

        public ReceiveDamageContext(CombatEntity source, CombatEntity target, int amount, string damageType, int tickIndex)
        {
            Source = source;
            Target = target;
            Amount = amount;
            DamageType = damageType;
            TickIndex = tickIndex;
        }
    }

    public sealed class DeathContext
    {
        public CombatEntity DeadEntity { get; private set; }
        public CombatEntity Killer { get; private set; }
        public int TickIndex { get; private set; }

        public DeathContext(CombatEntity deadEntity, CombatEntity killer, int tickIndex)
        {
            DeadEntity = deadEntity;
            Killer = killer;
            TickIndex = tickIndex;
        }
    }

    public sealed class CombatEventBus
    {
        public event Action<TurnStartContext> OnTurnStart;
        public event Action<AttackContext> OnAttackBefore;
        public event Action<DamageCalculationContext> OnDamageCalculate;
        public event Action<HitContext> OnHit;
        public event Action<ReceiveDamageContext> OnReceiveDamage;
        public event Action<DeathContext> OnDeath;

        public void RaiseTurnStart(TurnStartContext context) { OnTurnStart?.Invoke(context); }
        public void RaiseAttackBefore(AttackContext context) { OnAttackBefore?.Invoke(context); }
        public void RaiseDamageCalculate(DamageCalculationContext context) { OnDamageCalculate?.Invoke(context); }
        public void RaiseHit(HitContext context) { OnHit?.Invoke(context); }
        public void RaiseReceiveDamage(ReceiveDamageContext context) { OnReceiveDamage?.Invoke(context); }
        public void RaiseDeath(DeathContext context) { OnDeath?.Invoke(context); }
    }

    public class CombatEntity
    {
        private readonly MergeBoard mergeBoard;
        private readonly CombatStats baseStats;
        private CombatStats modifiers;

        public string Name { get; private set; }
        public bool IsPlayer { get; private set; }
        public int CurrentHP { get; private set; }
        public int ActionGauge { get; set; }
        public bool IsAlive { get { return CurrentHP > 0; } }

        public int ATK { get { return GetCurrentStats().ATK; } }
        public int DEF { get { return GetCurrentStats().DEF; } }
        public int Speed { get { return GetCurrentStats().Speed; } }
        public int MaxHP { get { return Math.Max(1, GetCurrentStats().MaxHP); } }
        public MergeBoard Board { get { return mergeBoard; } }

        private CombatEntity(string name, bool isPlayer, CombatStats baseStats, MergeBoard mergeBoard)
        {
            Name = name;
            IsPlayer = isPlayer;
            this.baseStats = baseStats;
            this.mergeBoard = mergeBoard;
            CurrentHP = Math.Max(1, GetCurrentStats().MaxHP);
        }

        public static CombatEntity CreatePlayer(string name, MergeBoard mergeBoard)
        {
            if (mergeBoard == null)
            {
                throw new ArgumentNullException(nameof(mergeBoard));
            }

            return new CombatEntity(name, true, new CombatStats(), mergeBoard);
        }

        public static CombatEntity CreateMonster(string name, CombatStats baseStats)
        {
            return new CombatEntity(name, false, baseStats, null);
        }

        public CombatStats GetCurrentStats()
        {
            CombatStats stats = IsPlayer && mergeBoard != null ? mergeBoard.GetAggregateStats() : baseStats;
            return stats + modifiers;
        }

        public void AddModifier(CombatStats modifier)
        {
            modifiers += modifier;
            ClampHealthToMax();
        }

        public int ReceiveDamage(CombatEntity source, int amount, string damageType, CombatEventBus eventBus, int tickIndex)
        {
            if (!IsAlive || amount <= 0)
            {
                return 0;
            }

            int damageTaken = Math.Min(CurrentHP, amount);
            CurrentHP -= damageTaken;

            if (eventBus != null)
            {
                eventBus.RaiseReceiveDamage(new ReceiveDamageContext(source, this, damageTaken, damageType, tickIndex));

                if (!IsAlive)
                {
                    eventBus.RaiseDeath(new DeathContext(this, source, tickIndex));
                }
            }

            return damageTaken;
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || !IsAlive)
            {
                return;
            }

            CurrentHP = Math.Min(MaxHP, CurrentHP + amount);
        }

        public void Revive(int amount)
        {
            if (amount <= 0 || IsAlive)
            {
                return;
            }

            CurrentHP = Math.Min(MaxHP, amount);
            ActionGauge = 0;
        }

        public void RefreshDynamicHealth()
        {
            ClampHealthToMax();
            if (CurrentHP <= 0)
            {
                CurrentHP = MaxHP;
            }
        }

        private void ClampHealthToMax()
        {
            CurrentHP = Math.Min(CurrentHP, MaxHP);
        }
    }
}
