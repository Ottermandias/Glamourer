using OtterGui.Classes;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

namespace Glamourer.Events;

/// <summary>
/// Triggered when a model flags a bonus slot for an update.
/// <list type="number">
///     <item>Parameter is the model with a flagged slot. </item>
///     <item>Parameter is the bonus slot changed. </item>
///     <item>Parameter is the model values to change the bonus piece to. </item>
///     <item>Parameter is the return value the function should return, if it is ulong.MaxValue, the original will be called and returned. </item>
/// </list>
/// </summary>
public sealed class BonusSlotUpdating()
    : EventWrapperRef34<Model, BonusItemFlag, CharacterArmor, ulong, BonusSlotUpdating.Priority>(nameof(BonusSlotUpdating))
{
    public enum Priority
    {
        /// <seealso cref="State.StateListener.OnBonusSlotUpdating"/>
        StateListener = 0,
    }
}
