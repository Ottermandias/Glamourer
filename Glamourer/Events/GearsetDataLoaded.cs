using OtterGui.Classes;
using Penumbra.GameData.Interop;

namespace Glamourer.Events;

/// <summary>
/// Triggers when the equipped gearset finished all LoadEquipment, LoadWeapon, and LoadCrest calls. (All Non-MetaData)
/// This defines an endpoint for when the gameState is updated.
/// <list type="number">
///     <item>The model draw object associated with the finished load (Also fired by other players on render) </item>
/// </list>
/// </summary>
public sealed class GearsetDataLoaded()
    : EventWrapper<Model, GearsetDataLoaded.Priority>(nameof(GearsetDataLoaded))
{
    public enum Priority
    {
        /// <seealso cref="State.StateListener.OnGearsetDataLoaded"/>
        StateListener = 0,
    }
}
