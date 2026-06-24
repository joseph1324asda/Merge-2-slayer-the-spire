using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Merge2.Combat
{
    public sealed class CombatManager : MonoBehaviour
    {
        private const int ActionThreshold = 1000;

        [SerializeField] private bool autoStart = true;
        [SerializeField] private float tickIntervalSeconds = 0.2f;

        private readonly List<CombatEntity> combatants = new List<CombatEntity>();
        private Coroutine combatRoutine;
        private int tickIndex;

        public CombatEventBus EventBus { get; private set; } = new CombatEventBus();
        public CombatEntity Player { get; private set; }
        public CombatEntity Monster { get; private set; }

        private void Awake()
        {
            EventBus.OnReceiveDamage += HandleReceiveDamageLog;
            EventBus.OnDeath += HandleDeathLog;
        }

        private void Start()
        {
            if (autoStart)
            {
                BuildPrototypeBattle();
                StartCombat();
            }
        }

        private void OnDestroy()
        {
            StopCombat();
            UnregisterBoardEffects(Player);
            EventBus.OnReceiveDamage -= HandleReceiveDamageLog;
            EventBus.OnDeath -= HandleDeathLog;
        }

        public void StartCombat()
        {
            if (combatRoutine != null)
            {
                return;
            }

            combatRoutine = StartCoroutine(TickLoop());
            Debug.Log("<color=white>[Combat]</color> Started tick-based ATB combat.");
        }

        public void StopCombat()
        {
            if (combatRoutine == null)
            {
                return;
            }

            StopCoroutine(combatRoutine);
            combatRoutine = null;
            Debug.Log("<color=white>[Combat]</color> Stopped.");
        }

        public void InitializeBattle(CombatEntity player, CombatEntity monster)
        {
            UnregisterBoardEffects(Player);

            Player = player;
            Monster = monster;
            combatants.Clear();

            if (Player != null)
            {
                Player.RefreshDynamicHealth();
                RegisterBoardEffects(Player);
                combatants.Add(Player);
            }

            if (Monster != null)
            {
                combatants.Add(Monster);
            }

            tickIndex = 0;
        }

        private IEnumerator TickLoop()
        {
            WaitForSeconds wait = new WaitForSeconds(tickIntervalSeconds);

            while (HasLivingSide())
            {
                tickIndex++;
                Debug.Log("<color=grey>[Tick " + tickIndex + "]</color>");

                for (int i = 0; i < combatants.Count; i++)
                {
                    CombatEntity entity = combatants[i];
                    if (entity == null || !entity.IsAlive)
                    {
                        continue;
                    }

                    entity.ActionGauge += Mathf.Max(0, entity.Speed);
                    Debug.Log("<color=grey>[Gauge]</color> " + entity.Name + " +" + entity.Speed + " => " + entity.ActionGauge + "/" + ActionThreshold);

                    while (entity.ActionGauge >= ActionThreshold && entity.IsAlive && HasLivingSide())
                    {
                        entity.ActionGauge -= ActionThreshold;
                        TakeTurn(entity);
                    }
                }

                yield return wait;
            }

            Debug.Log("<color=yellow>[Combat]</color> Battle finished.");
            combatRoutine = null;
        }

        private void TakeTurn(CombatEntity actor)
        {
            CombatEntity target = GetDefaultTarget(actor);
            if (target == null)
            {
                return;
            }

            EventBus.RaiseTurnStart(new TurnStartContext(actor, tickIndex));
            Debug.Log("<color=cyan>[Turn]</color> " + actor.Name + " acts against " + target.Name + ".");

            PerformBasicAttack(actor, target);
        }

        private void PerformBasicAttack(CombatEntity attacker, CombatEntity target)
        {
            EventBus.RaiseAttackBefore(new AttackContext(attacker, target, tickIndex));

            int rawAttack = attacker.ATK;
            int targetDefense = target.DEF;
            int baseDamage = Mathf.Max(1, rawAttack - targetDefense);
            DamageCalculationContext damageContext = new DamageCalculationContext(attacker, target, rawAttack, targetDefense, baseDamage, tickIndex);

            EventBus.RaiseDamageCalculate(damageContext);

            int finalDamage = Mathf.Max(1, damageContext.DamageAmount);
            int dealt = target.ReceiveDamage(attacker, finalDamage, "MainAttack", EventBus, tickIndex);

            Debug.Log("<color=red>[Main Damage]</color> " + attacker.Name + " ATK " + rawAttack + " vs " + target.Name + " DEF " + targetDefense + " => <color=red>" + dealt + "</color> damage. " + target.Name + " HP " + target.CurrentHP + "/" + target.MaxHP);

            EventBus.RaiseHit(new HitContext(attacker, target, dealt, tickIndex));
        }

        private CombatEntity GetDefaultTarget(CombatEntity actor)
        {
            if (actor == Player)
            {
                return Monster != null && Monster.IsAlive ? Monster : null;
            }

            return Player != null && Player.IsAlive ? Player : null;
        }

        private bool HasLivingSide()
        {
            return Player != null && Monster != null && Player.IsAlive && Monster.IsAlive;
        }

        private void RegisterBoardEffects(CombatEntity entity)
        {
            if (entity == null || entity.Board == null)
            {
                return;
            }

            IReadOnlyList<EquipmentItem> items = entity.Board.EquipmentItems;
            for (int i = 0; i < items.Count; i++)
            {
                EquipmentItem item = items[i];
                for (int j = 0; j < item.Effects.Count; j++)
                {
                    item.Effects[j].Register(EventBus, item);
                    Debug.Log("<color=lime>[Effect Registered]</color> " + item.ID + " -> " + item.Effects[j].EffectName);
                }
            }
        }

        private void UnregisterBoardEffects(CombatEntity entity)
        {
            if (entity == null || entity.Board == null)
            {
                return;
            }

            foreach (ICombatEffect effect in entity.Board.GetAllEffects())
            {
                effect.Unregister();
            }
        }

        private void BuildPrototypeBattle()
        {
            MergeBoard board = new MergeBoard();

            EquipmentItem sword = new EquipmentItem("IronSword_L1", 1, 14, 0, 70, 15);
            sword.AddEffect(new PoisonEffect(3));

            EquipmentItem rod = new EquipmentItem("StormRod_L1", 1, 6, 0, 35, 8);
            rod.AddEffect(new LightningStrikeEffect(8, 0.35f));

            EquipmentItem shield = new EquipmentItem("GuardPlate_L1", 1, 0, 4, 15, 22);

            board.AddEquipment(sword);
            board.AddEquipment(rod);
            board.AddEquipment(shield);

            CombatEntity player = CombatEntity.CreatePlayer("Player", board);
            CombatEntity monster = CombatEntity.CreateMonster("Training Slime", new CombatStats(9, 5, 120, 75));

            InitializeBattle(player, monster);

            CombatStats aggregate = board.GetAggregateStats();
            Debug.Log("<color=orange>[Board Stats]</color> ATK " + aggregate.ATK + ", DEF " + aggregate.DEF + ", Speed " + aggregate.Speed + ", MaxHP " + aggregate.MaxHP);
        }

        private void HandleReceiveDamageLog(ReceiveDamageContext context)
        {
            string sourceName = context.Source != null ? context.Source.Name : "Unknown";
            Debug.Log("<color=#FF8888>[Receive Damage]</color> " + context.Target.Name + " receives " + context.Amount + " " + context.DamageType + " damage from " + sourceName + " on tick " + context.TickIndex + ".");
        }

        private void HandleDeathLog(DeathContext context)
        {
            string killerName = context.Killer != null ? context.Killer.Name : "Unknown";
            Debug.Log("<color=magenta>[Death]</color> " + context.DeadEntity.Name + " was defeated by " + killerName + " on tick " + context.TickIndex + ".");
        }
    }
}
