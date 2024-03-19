using OtterGui.Classes;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

namespace Glamourer.Events;

/// <summary>
/// Triggered when a model flags an equipment slot for an update.
/// <list type="number">
///     <item>Parameter is the model with a flagged slot. </item>
///     <item>Parameter is the equipment slot changed. </item>
///     <item>Parameter is the model values to change the equipment piece to. </item>
///     <item>Parameter is the return value the function should return, if it is ulong.MaxValue, the original will be called and returned. </item>
/// </list>
/// </summary>
public sealed class SlotUpdating() 
    : EventWrapperRef34<Model, EquipSlot, CharacterArmor, ulong, SlotUpdating.Priority>(nameof(SlotUpdating))
{
    public enum Priority
    {
        /// <seealso cref="State.StateListener.OnSlotUpdating"/>
        StateListener = 0,
    }
}
