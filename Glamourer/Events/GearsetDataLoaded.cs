using OtterGui.Classes;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

namespace Glamourer.Events;

/// <summary>
/// Triggers when the equipped gearset finished running all of its LoadEquipment, LoadWeapon, and crest calls.
/// This defines a universal endpoint of base game state application to monitor.
/// <list type="number">
///     <item>The model drawobject associated with the finished load (should always be ClientPlayer) </item>
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