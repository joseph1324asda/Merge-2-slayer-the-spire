using UnityEngine;

namespace Merge2.Combat
{
    public sealed class PoisonEffect : ICombatEffect
    {
        private CombatEventBus eventBus;

        public string EffectName { get { return "Poison"; } }
        public EquipmentItem Owner { get; private set; }
        public int DamagePerHit { get; private set; }

        public PoisonEffect(int damagePerHit)
        {
            DamagePerHit = Mathf.Max(1, damagePerHit);
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

            int dealt = context.Target.ReceiveDamage(context.Attacker, DamagePerHit, "Poison", eventBus, context.TickIndex);
            Debug.Log("<color=green>[Effect: Poison]</color> " + Owner.ID + " deals <color=green>" + dealt + "</color> poison damage to " + context.Target.Name + ".");
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
