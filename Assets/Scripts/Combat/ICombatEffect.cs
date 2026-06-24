using System;

namespace Merge2.Combat
{
    public interface ICombatEffect : IDisposable
    {
        string EffectName { get; }
        EquipmentItem Owner { get; }

        void Register(CombatEventBus eventBus, EquipmentItem owner);
        void Unregister();
    }
}
