using Luna;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

namespace Glamourer.Events;

/// <summary> Triggered when a model flags an equipment slot for an update. </summary>
public sealed class WeaponLoading(Logger log)
    : EventBase<WeaponLoading.Arguments, WeaponLoading.Priority>(nameof(WeaponLoading), log)
{
    public enum Priority
    {
        /// <seealso cref="State.StateListener.OnWeaponLoading"/>
        StateListener = 0,

        /// <seealso cref="Automation.AutoDesignApplier.OnWeaponLoading"/>
        AutoDesignApplier = -1,
    }

    public ref struct Arguments(Actor actor, EquipSlot slot, ref CharacterWeapon weapon)
    {
        /// <summary> The actor that has its weapons changed. </summary>
        public readonly Actor Actor = actor;

        /// <summary> The changed equipment slot (either Mainhand or Offhand). </summary>
        public readonly EquipSlot Slot = slot;

        /// <summary> The model data for the new weapon. </summary>
        public ref CharacterWeapon Weapon = ref weapon;
    }
}
