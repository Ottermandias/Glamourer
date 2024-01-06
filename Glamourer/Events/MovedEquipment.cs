using OtterGui.Classes;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Events;

/// <summary>
/// Triggered when a game object updates an equipment piece in its model data.
/// <list type="number">
///     <item>Parameter is an array of slots updated and corresponding item ids and stains. </item>
/// </list>
/// </summary>
public sealed class MovedEquipment() 
    : EventWrapper<(EquipSlot, uint, StainId)[], MovedEquipment.Priority>(nameof(MovedEquipment))
{
    public enum Priority
    {
        /// <seealso cref="State.StateListener.OnMovedEquipment"/>
        StateListener = 0,
    }
}
