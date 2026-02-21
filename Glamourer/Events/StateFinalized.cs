using Glamourer.Api;
using Glamourer.Api.Enums;
using Luna;
using Penumbra.GameData.Interop;

namespace Glamourer.Events;

/// <summary> Triggered when a set of grouped changes finishes being applied to a Glamourer state. </summary>
public sealed class StateFinalized(Logger log)
    : EventBase<StateFinalized.Arguments, StateFinalized.Priority>(nameof(StateFinalized), log)
{
    public enum Priority
    {
        /// <seealso cref="StateApi.OnStateFinalized"/>
        StateApi = int.MinValue,
    }

    /// <summary> Arguments for the StateFinalized event. </summary>
    /// <param name="Type"> The operation that finished updating the saved state. </param>
    /// <param name="Actors"> The existing actors using this saved state at the moment. </param>
    public readonly record struct Arguments(StateFinalizationType Type, ActorData Actors);
}
