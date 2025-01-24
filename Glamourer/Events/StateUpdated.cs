using Glamourer.Api.Enums;
using Glamourer.Designs.History;
using Glamourer.Interop.Structs;
using Glamourer.State;
using OtterGui.Classes;

namespace Glamourer.Events;

/// <summary>
/// Triggered when a Design is edited in any way.
/// <list type="number">
///     <item>Parameter is the operation that finished updating the saved state. </item>
///     <item>Parameter is the existing actors using this saved state. </item>
/// </list>
/// </summary>
public sealed class StateUpdated()
    : EventWrapper<StateFinalizationType, ActorData, StateUpdated.Priority>(nameof(StateUpdated))
{
    public enum Priority
    {
        /// <seealso cref="Api.GlamourerIpc.OnStateUpdated"/>
        GlamourerIpc = int.MinValue,
    }
}
