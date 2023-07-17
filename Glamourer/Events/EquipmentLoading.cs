using System;
using Glamourer.Interop.Structs;
using OtterGui.Classes;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Events;

/// <summary>
/// Triggered when a game object updates an equipment piece in its model data.
/// <list type="number">
///     <item>Parameter is the character updating. </item>
///     <item>Parameter is the equipment slot changed. </item>
///     <item>Parameter is the model values to change the equipment piece to. </item>
/// </list>
/// </summary>
public sealed class EquipmentLoading : EventWrapper<Action<Actor, EquipSlot, CharacterArmor>, EquipmentLoading.Priority>
{
    public enum Priority
    {
        /// <seealso cref="State.StateListener.OnEquipmentLoading"/>
        StateListener = 0,
    }

    public EquipmentLoading()
        : base(nameof(EquipmentLoading))
    { }

    public void Invoke(Actor actor, EquipSlot slot, CharacterArmor armor)
        => Invoke(this, actor, slot, armor);
}
