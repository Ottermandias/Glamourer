using System;
using Glamourer.Interop.Structs;
using OtterGui.Classes;

namespace Glamourer.Events;

/// <summary>
/// Triggered when the visibility of weapons is changed.
/// <list type="number">
///     <item>Parameter is the actor with changed weapon visibility. </item>
///     <item>Parameter is the new state. </item>
/// </list>
/// </summary>
public sealed class WeaponVisibilityChanged : EventWrapper<Action<Actor, Ref<bool>>, WeaponVisibilityChanged.Priority>
{
    public enum Priority
    {
        /// <seealso cref="State.StateListener.OnWeaponVisibilityChange"/>
        StateListener = 0,
    }

    public WeaponVisibilityChanged()
        : base(nameof(WeaponVisibilityChanged))
    { }

    public void Invoke(Actor actor, ref bool state)
    {
        var value = new Ref<bool>(state);
        Invoke(this, actor, value);
        state = value;
    }
}
