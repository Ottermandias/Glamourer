using OtterGui.Classes;
using Penumbra.GameData.Interop;

namespace Glamourer.Events;

/// <summary>
/// Triggered when the visibility of weapons is changed.
/// <list type="number">
///     <item>Parameter is the actor with changed weapon visibility. </item>
///     <item>Parameter is the new state. </item>
/// </list>
/// </summary>
public sealed class WeaponVisibilityChanged() : EventWrapperRef2<Actor, bool, WeaponVisibilityChanged.Priority>(nameof(WeaponVisibilityChanged))
{
    public enum Priority
    {
        /// <seealso cref="State.StateListener.OnWeaponVisibilityChange"/>
        StateListener = 0,
    }
}
