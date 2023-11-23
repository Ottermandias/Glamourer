using System;
using Glamourer.Interop.Structs;
using OtterGui.Classes;
using Penumbra.GameData.Enums;

namespace Glamourer.Events;

/// <summary>
/// Triggered when the crest visibility is updated on a model.
/// <list type="number">
///     <item>Parameter is the model with an update. </item>
///     <item>Parameter is the equipment slot changed. </item>
///     <item>Parameter is the whether the crest will be shown. </item>
/// </list>
/// </summary>
public sealed class CrestVisibilityUpdating : EventWrapper<Action<Model, EquipSlot, Ref<bool>>, CrestVisibilityUpdating.Priority>
{
    public enum Priority
    {
        /// <seealso cref="State.StateListener.OnCrestVisibilityUpdating"/>
        StateListener = 0,
    }

    public CrestVisibilityUpdating()
        : base(nameof(CrestVisibilityUpdating))
    { }

    public void Invoke(Model model, EquipSlot slot, ref bool visible)
    {
        var @return = new Ref<bool>(visible);
        Invoke(this, model, slot, @return);
        visible = @return;
    }
}
