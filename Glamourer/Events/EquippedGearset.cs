using OtterGui.Classes;

namespace Glamourer.Events;

/// <summary>
/// Triggered when the player equips a gear set.
/// <list type="number">
///     <item>Parameter is the name of the gear set. </item>
///     <item>Parameter is the id of the gear set. </item>
///     <item>Parameter is the id of the prior gear set. </item>
///     <item>Parameter is the id of the associated glamour. </item>
///     <item>Parameter is the job id of the associated job. </item>
/// </list>
/// </summary>
public sealed class EquippedGearset() 
    : EventWrapper<string, int, int, byte, byte, EquippedGearset.Priority>(nameof(EquippedGearset))
{
    public enum Priority
    {
        /// <seealso cref="Automation.AutoDesignApplier.OnEquippedGearset"/>
        AutoDesignApplier = 0,
    }
}
