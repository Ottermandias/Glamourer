using System;
using Glamourer.Interop.Structs;
using OtterGui.Classes;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Events;

/// <summary>
/// Triggered when a model flags an equipment slot for an update.
/// <list type="number">
///     <item>Parameter is the actor that has its weapons changed. </item>
///     <item>Parameter is the equipment slot changed (Mainhand or Offhand). </item>
///     <item>Parameter is the model values to change the weapon to. </item>
/// </list>
/// </summary>
public sealed class WeaponLoading : EventWrapper<Action<Actor, EquipSlot, Ref<CharacterWeapon>>, WeaponLoading.Priority>
{
    public enum Priority
    {
        /// <seealso cref="State.StateListener.OnWeaponLoading"/>
        StateListener = 0,

        /// <seealso cref="Automation.AutoDesignApplier.OnWeaponLoading"/>
        AutoDesignApplier = -1,
    }

    public WeaponLoading()
        : base(nameof(WeaponLoading))
    { }

    public void Invoke(Actor actor, EquipSlot slot, ref CharacterWeapon weapon)
    {
        var value = new Ref<CharacterWeapon>(weapon);
        Invoke(this, actor, slot, value);
        weapon = value;
    }
}
