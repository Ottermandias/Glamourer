using System;
using Glamourer.Interop.Structs;
using OtterGui.Classes;
using Penumbra.GameData.Enums;
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
public sealed class UpdatedSlot : EventWrapper<Action<Model, EquipSlot, Ref<CharacterArmor>, Ref<ulong>>, UpdatedSlot.Priority>
{
    public enum Priority
    {
        /// <seealso cref="State.StateManager.OnSlotUpdated"/>
        StateManager = 0,
    }

    public UpdatedSlot()
        : base(nameof(UpdatedSlot))
    { }

    public void Invoke(Model model, EquipSlot slot, ref CharacterArmor armor, ref ulong returnValue)
    {
        var value   = new Ref<CharacterArmor>(armor);
        var @return = new Ref<ulong>(returnValue);
        Invoke(this, model, slot, value, @return);
        armor       = value;
        returnValue = @return;
    }
}
