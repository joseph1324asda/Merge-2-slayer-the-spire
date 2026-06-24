using System.Collections.Generic;

namespace Merge2.Combat
{
    public struct CombatStats
    {
        public int ATK;
        public int DEF;
        public int Speed;
        public int MaxHP;

        public CombatStats(int atk, int def, int speed, int maxHP)
        {
            ATK = atk;
            DEF = def;
            Speed = speed;
            MaxHP = maxHP;
        }

        public static CombatStats operator +(CombatStats left, CombatStats right)
        {
            return new CombatStats(
                left.ATK + right.ATK,
                left.DEF + right.DEF,
                left.Speed + right.Speed,
                left.MaxHP + right.MaxHP);
        }
    }

    public sealed class MergeBoard
    {
        private readonly List<EquipmentItem> equipmentItems = new List<EquipmentItem>();

        public IReadOnlyList<EquipmentItem> EquipmentItems { get { return equipmentItems; } }

        public void AddEquipment(EquipmentItem item)
        {
            if (item == null || equipmentItems.Contains(item))
            {
                return;
            }

            equipmentItems.Add(item);
        }

        public bool RemoveEquipment(EquipmentItem item)
        {
            if (item == null)
            {
                return false;
            }

            return equipmentItems.Remove(item);
        }

        public CombatStats GetAggregateStats()
        {
            CombatStats total = new CombatStats();

            for (int i = 0; i < equipmentItems.Count; i++)
            {
                total += equipmentItems[i].GetStats();
            }

            return total;
        }

        public IEnumerable<ICombatEffect> GetAllEffects()
        {
            for (int i = 0; i < equipmentItems.Count; i++)
            {
                IReadOnlyList<ICombatEffect> effects = equipmentItems[i].Effects;
                for (int j = 0; j < effects.Count; j++)
                {
                    yield return effects[j];
                }
            }
        }
    }
}
