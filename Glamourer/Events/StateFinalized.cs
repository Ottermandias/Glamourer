using Glamourer.Api;
using Glamourer.Api.Enums;
using Glamourer.Interop.Structs;
using OtterGui.Classes;

namespace Glamourer.Events;

/// <summary>
/// Triggered when a set of grouped changes finishes being applied to a Glamourer state.
/// <list type="number">
///     <item>Parameter is the operation that finished updating the saved state. </item>
///     <item>Parameter is the existing actors using this saved state. </item>
/// </list>
/// </summary>
public sealed class StateFinalized()
    : EventWrapper<StateFinalizationType, ActorData, StateFinalized.Priority>(nameof(StateFinalized))
{
    public enum Priority
    {
        /// <seealso cref="StateApi.OnStateFinalized"/>
        StateApi = int.MinValue,
    }
}
