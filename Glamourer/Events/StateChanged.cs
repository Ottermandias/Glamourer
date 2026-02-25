using Glamourer.Api.Enums;
using Glamourer.Designs.History;
using Glamourer.State;
using Penumbra.GameData.Interop;
using Luna;

namespace Glamourer.Events;

/// <summary> Triggered when a state changes in any way. </summary>
public sealed class StateChanged(Logger log)
    : EventBase<StateChanged.Arguments, StateChanged.Priority>(nameof(StateChanged), log)
{
    public enum Priority
    {
        /// <seealso cref="Api.StateApi.OnStateChanged" />
        GlamourerIpc = int.MinValue,

        /// <seealso cref="global::Glamourer.Interop.Penumbra.PenumbraAutoRedraw.OnStateChanged" />
        PenumbraAutoRedraw = 0,

        /// <seealso cref="EditorHistory.OnStateChanged" />
        EditorHistory = -1000,
    }

    /// <summary> Arguments for the StateChanged event. </summary>
    /// <param name="Type"> The type of change that occured. </param>
    /// <param name="Source"> The source of the change. </param>
    /// <param name="State"> The changed state. </param>
    /// <param name="Actors"> Any currently affected actors. </param>
    /// <param name="Transaction"> Additional data depending on the type of change. </param>
    public readonly record struct Arguments(
        StateChangeType Type,
        StateSource Source,
        ActorState State,
        ActorData Actors,
        ITransaction? Transaction = null);
}
