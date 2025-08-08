using OtterGui.Classes;
using Penumbra.GameData.Interop;

namespace Glamourer.Events;

/// <summary>
/// Triggered when the state of viera ear visibility for any draw object is changed.
/// <list type="number">
///     <item>Parameter is the model with a changed viera ear visibility state. </item>
///     <item>Parameter is the new state. </item>
///     <item>Parameter is whether to call the original function. </item>
/// </list>
/// </summary>
public sealed class VieraEarStateChanged()
    : EventWrapperRef2<Actor, bool, VieraEarStateChanged.Priority>(nameof(VieraEarStateChanged))
{
    public enum Priority
    {
        /// <seealso cref="State.StateListener.OnVieraEarChange"/>
        StateListener = 0,
    }
}
