using Luna;
using Penumbra.GameData.Interop;

namespace Glamourer.Events;

/// <summary> Triggered when the visibility of head gear is changed. </summary>
public sealed class HeadGearVisibilityChanged(Logger log)
    : EventBase<HeadGearVisibilityChanged.Arguments, HeadGearVisibilityChanged.Priority>(nameof(HeadGearVisibilityChanged), log)
{
    public enum Priority
    {
        /// <seealso cref="State.StateListener.OnHeadGearVisibilityChange"/>
        StateListener = 0,
    }

    public ref struct Arguments(Actor actor, ref bool visible)
    {
        /// <summary> The actor whose head gear visibility changed. </summary>
        public readonly Actor Actor = actor;

        /// <summary> The new visibility state. </summary>
        public ref bool Visible = ref visible;
    }
}
