using Luna;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

namespace Glamourer.Events;

/// <summary> Triggered when a model flags a bonus slot for an update. </summary>
public sealed class BonusSlotUpdating(Logger log)
    : EventBase<BonusSlotUpdating.Arguments, BonusSlotUpdating.Priority>(nameof(BonusSlotUpdating), log)
{
    public enum Priority
    {
        /// <seealso cref="State.StateListener.OnBonusSlotUpdating"/>
        StateListener = 0,
    }

    public ref struct Arguments(Model model, BonusItemFlag flag, ref CharacterArmor armor, ref ulong returnValue)
    {
        /// <summary> The draw object with an updated bonus slot. </summary>
        public readonly Model Model = model;

        /// <summary> The updated slot. </summary>
        public readonly BonusItemFlag Slot = flag;

        /// <summary> The model data for the new bonus slot. This can be changed. </summary>
        public ref CharacterArmor Armor = ref armor;

        /// <summary> The return value of the event. If this is <see cref="ulong.MaxValue"/>, the original function will be called and its return value used, otherwise this value will be returned. </summary>
        public ref ulong ReturnValue = ref returnValue;
    }
}
