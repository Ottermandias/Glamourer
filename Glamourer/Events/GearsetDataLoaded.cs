using OtterGui.Classes;
using Penumbra.GameData.Interop;

namespace Glamourer.Events;

/// <summary>
/// Triggers when the equipped gearset finished running all of its LoadEquipment, LoadWeapon, and crest calls.
/// This defines a universal endpoint of base game state application to monitor.
/// <list type="number">
///     <item>The model drawobject associated with the finished load (Also fired by other players on render) </item>
/// </list>
/// </summary>
public sealed class GearsetDataLoaded()
    : EventWrapper<Model, GearsetDataLoaded.Priority>(nameof(GearsetDataLoaded))
{
    public enum Priority
    {
        /// <seealso cref="State.StateListener.OnEquippedGearsetLoaded"/>
        StateListener = 0,
    }
}