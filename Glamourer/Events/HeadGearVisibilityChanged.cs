using OtterGui.Classes;
using Penumbra.GameData.Interop;

namespace Glamourer.Events;

/// <summary>
/// Triggered when the visibility of head gear is changed.
/// <list type="number">
///     <item>Parameter is the actor with changed head gear visibility. </item>
///     <item>Parameter is the new state. </item>
/// </list>
/// </summary>
public sealed class HeadGearVisibilityChanged()
    : EventWrapperRef2<Actor, bool, HeadGearVisibilityChanged.Priority>(nameof(HeadGearVisibilityChanged))
{
    public enum Priority
    {
        /// <seealso cref="State.StateListener.OnHeadGearVisibilityChange"/>
        StateListener = 0,
    }
}
