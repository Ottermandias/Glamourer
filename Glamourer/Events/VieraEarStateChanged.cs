using Luna;
using Penumbra.GameData.Interop;

namespace Glamourer.Events;

/// <summary> Triggered when the state of viera ear visibility for any draw object is changed. </summary>
public sealed class VieraEarStateChanged(Logger log)
    : EventBase<VieraEarStateChanged.Arguments, VieraEarStateChanged.Priority>(nameof(VieraEarStateChanged), log)
{
    public enum Priority
    {
        /// <seealso cref="State.StateListener.OnVieraEarChange"/>
        StateListener = 0,
    }

    public ref struct Arguments(Actor actor, ref bool state)
    {
        /// <summary> The actor with a changed viera ear visibility state. </summary>
        public readonly Actor Actor = actor;

        /// <summary> The new ear visibility state. </summary>
        public ref bool State = ref state;
    }
}
