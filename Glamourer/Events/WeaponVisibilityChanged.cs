using Luna;
using Penumbra.GameData.Interop;

namespace Glamourer.Events;

/// <summary> Triggered when the visibility of weapons is changed. </summary>
public sealed class WeaponVisibilityChanged(Logger log)
    : EventBase<WeaponVisibilityChanged.Arguments, WeaponVisibilityChanged.Priority>(nameof(WeaponVisibilityChanged), log)
{
    public enum Priority
    {
        /// <seealso cref="State.StateListener.OnWeaponVisibilityChange"/>
        StateListener = 0,
    }

    public ref struct Arguments(Actor actor, ref bool value)
    {
        /// <summary> The actor with changed weapon visibility. </summary>
        public readonly Actor Actor = actor;

        /// <summary> The new state. </summary>
        public ref bool Value = ref value;
    }
}
