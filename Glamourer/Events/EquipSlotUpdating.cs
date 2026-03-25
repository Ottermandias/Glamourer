using Luna;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

namespace Glamourer.Events;

/// <summary> Triggered when a model flags an equipment slot for an update. </summary>
public sealed class EquipSlotUpdating(Logger log)
    : EventBase<EquipSlotUpdating.Arguments, EquipSlotUpdating.Priority>(nameof(EquipSlotUpdating), log)
{
    public enum Priority
    {
        /// <seealso cref="State.StateListener.OnEquipSlotUpdating"/>
        StateListener = 0,
    }

    public ref struct Arguments(Model model, EquipSlot slot, ref CharacterArmor armor, ref ulong returnValue)
    {
        /// <summary> The draw object with an updated equipment slot. </summary>
        public readonly Model Model = model;

        /// <summary> The updated slot. </summary>
        public readonly EquipSlot Slot = slot;

        /// <summary> The model data for the new equipment slot. This can be changed. </summary>
        public ref CharacterArmor Armor = ref armor;

        /// <summary> The return value of the event. If this is <see cref="ulong.MaxValue"/>, the original function will be called and its return value used, otherwise this value will be returned. </summary>
        public ref ulong ReturnValue = ref returnValue;
    }
}