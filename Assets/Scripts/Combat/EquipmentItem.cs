using System.Collections.Generic;

namespace Merge2.Combat
{
    public sealed class EquipmentItem
    {
        private readonly List<ICombatEffect> effects = new List<ICombatEffect>();

        public string ID { get; private set; }
        public int Level { get; private set; }
        public int BaseATK { get; private set; }
        public int BaseDEF { get; private set; }
        public int BaseSpeed { get; private set; }
        public int BaseHP { get; private set; }
        public IReadOnlyList<ICombatEffect> Effects { get { return effects; } }

        public EquipmentItem(string id, int level, int baseATK, int baseDEF, int baseSpeed, int baseHP)
        {
            ID = id;
            Level = level;
            BaseATK = baseATK;
            BaseDEF = baseDEF;
            BaseSpeed = baseSpeed;
            BaseHP = baseHP;
        }

        public void AddEffect(ICombatEffect effect)
        {
            if (effect == null || effects.Contains(effect))
            {
                return;
            }

            effects.Add(effect);
        }

        public void RemoveEffect(ICombatEffect effect)
        {
            if (effect == null)
            {
                return;
            }

            if (effects.Remove(effect))
            {
                effect.Unregister();
            }
        }

        public CombatStats GetStats()
        {
            return new CombatStats(BaseATK, BaseDEF, BaseSpeed, BaseHP);
        }
    }
}
