using Luna;
using Penumbra.GameData.Interop;

namespace Glamourer.Events;

/// <summary> Triggered when the state of a visor for any draw object is changed. </summary>
public sealed class VisorStateChanged(Logger log)
    : EventBase<VisorStateChanged.Arguments, VisorStateChanged.Priority>(nameof(VisorStateChanged), log)
{
    public enum Priority
    {
        /// <seealso cref="State.StateListener.OnVisorChange"/>
        StateListener = 0,
    }

    public ref struct Arguments(Model model, bool visorState, ref bool value)
    {
        /// <summary> The model with a changed visor state. </summary>
        public readonly Model Model = model;

        /// <summary> The game's visor state. </summary>
        public readonly bool NewVisorState = visorState;

        /// <summary> The actual new value </summary>
        public ref bool Value = ref value;
    }
}
