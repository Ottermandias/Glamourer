using OtterGui.Classes;
using Penumbra.GameData.Interop;

namespace Glamourer.Events;

/// <summary>
/// Triggered when the state of a visor for any draw object is changed.
/// <list type="number">
///     <item>Parameter is the model with a changed visor state. </item>
///     <item>Parameter is the new state. </item>
///     <item>Parameter is whether to call the original function. </item>
/// </list>
/// </summary>
public sealed class VisorStateChanged()
    : EventWrapperRef3<Model, bool, bool, VisorStateChanged.Priority>(nameof(VisorStateChanged))
{
    public enum Priority
    {
        /// <seealso cref="State.StateListener.OnVisorChange"/>
        StateListener = 0,
    }
}
