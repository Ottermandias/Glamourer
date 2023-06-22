using System;
using Glamourer.Interop.Structs;
using OtterGui.Classes;

namespace Glamourer.Events;

/// <summary>
/// Triggered when the visibility of head gear is changed.
/// <list type="number">
///     <item>Parameter is the actor with changed head gear visibility. </item>
///     <item>Parameter is the new state. </item>
/// </list>
/// </summary>
public sealed class HeadGearVisibilityChanged : EventWrapper<Action<Actor, Ref<bool>>, HeadGearVisibilityChanged.Priority>
{
    public enum Priority
    {
        /// <seealso cref="State.StateListener.OnHeadGearVisibilityChange"/>
        StateListener = 0,
    }

    public HeadGearVisibilityChanged()
        : base(nameof(HeadGearVisibilityChanged))
    { }

    public void Invoke(Actor actor, ref bool state)
    {
        var value = new Ref<bool>(state);
        Invoke(this, actor, value);
        state = value;
    }
}
