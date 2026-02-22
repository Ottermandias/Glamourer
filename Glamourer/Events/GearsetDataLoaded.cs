using Luna;
using Penumbra.GameData.Interop;

namespace Glamourer.Events;

/// <summary>
///   Triggers when the equipped gearset finished all LoadEquipment, LoadWeapon, and LoadCrest calls. (All Non-MetaData)
///   This defines an endpoint for when the gameState is updated.
/// </summary>
public sealed class GearsetDataLoaded(Logger log)
    : EventBase<GearsetDataLoaded.Arguments, GearsetDataLoaded.Priority>(nameof(GearsetDataLoaded), log)
{
    public enum Priority
    {
        /// <seealso cref="State.StateListener.OnGearsetDataLoaded"/>
        StateListener = 0,
    }

    /// <summary> Arguments for the GearsetDataLoaded event. </summary>
    /// <param name="Actor"> The actor that loaded a gear set. </param>
    /// <param name="Model"> The draw object that finished the load. </param>
    public readonly record struct Arguments(Actor Actor, Model Model);
}
