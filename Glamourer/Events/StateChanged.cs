using Glamourer.Api.Enums;
using Glamourer.Interop.Structs;
using Glamourer.State;
using OtterGui.Classes;

namespace Glamourer.Events;

/// <summary>
/// Triggered when a Design is edited in any way.
/// <list type="number">
///     <item>Parameter is the type of the change </item>
///     <item>Parameter is the changed saved state. </item>
///     <item>Parameter is the existing actors using this saved state. </item>
///     <item>Parameter is any additional data depending on the type of change. </item>
/// </list>
/// </summary>
public sealed class StateChanged()
    : EventWrapper<StateChangeType, StateSource, ActorState, ActorData, object?, StateChanged.Priority>(nameof(StateChanged))
{
    public enum Priority
    {
        GlamourerIpc       = int.MinValue,
        PenumbraAutoRedraw = 0,
    }
}