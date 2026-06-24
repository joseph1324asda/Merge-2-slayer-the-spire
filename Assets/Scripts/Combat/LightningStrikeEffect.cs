using UnityEngine;

namespace Merge2.Combat
{
    public sealed class LightningStrikeEffect : ICombatEffect
    {
        private CombatEventBus eventBus;

        public string EffectName { get { return "Lightning Strike"; } }
        public EquipmentItem Owner { get; private set; }
        public int Damage { get; private set; }
        public float TriggerChance { get; private set; }

        public LightningStrikeEffect(int damage, float triggerChance)
        {
            Damage = Mathf.Max(1, damage);
            TriggerChance = Mathf.Clamp01(triggerChance);
        }

        public void Register(CombatEventBus eventBus, EquipmentItem owner)
        {
            Unregister();

            this.eventBus = eventBus;
            Owner = owner;

            if (this.eventBus != null)
            {
                this.eventBus.OnHit += HandleHit;
            }
        }

        public void Unregister()
        {
            if (eventBus != null)
            {
                eventBus.OnHit -= HandleHit;
            }

            eventBus = null;
            Owner = null;
        }

        public void Dispose()
        {
            Unregister();
        }

        private void HandleHit(HitContext context)
        {
            if (Owner == null || context.Attacker == null || !context.Attacker.IsPlayer || context.Attacker.Board == null)
            {
                return;
            }

            if (!ContainsOwner(context.Attacker.Board) || context.Target == null || !context.Target.IsAlive)
            {
                return;
            }

            if (Random.value > TriggerChance)
            {
                Debug.Log("<color=#88CCFF>[Effect: Lightning]</color> " + Owner.ID + " did not trigger.");
                return;
            }

            int dealt = context.Target.ReceiveDamage(context.Attacker, Damage, "Lightning", eventBus, context.TickIndex);
            Debug.Log("<color=#88CCFF>[Effect: Lightning]</color> " + Owner.ID + " strikes " + context.Target.Name + " for <color=#88CCFF>" + dealt + "</color> damage.");
        }

        private bool ContainsOwner(MergeBoard board)
        {
            for (int i = 0; i < board.EquipmentItems.Count; i++)
            {
                if (ReferenceEquals(board.EquipmentItems[i], Owner))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
