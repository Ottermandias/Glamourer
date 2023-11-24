using System;
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
public sealed class EquippedGearset : EventWrapper<Action<string, int, int, byte, byte>, EquippedGearset.Priority>
{
    public enum Priority
    {
        /// <seealso cref="Automation.AutoDesignApplier.OnEquippedGearset"/>
        AutoDesignApplier = 0,
    }

    public EquippedGearset()
        : base(nameof(EquippedGearset))
    { }

    public void Invoke(string name, int id, int lastId, byte glamour, byte jobId)
        => Invoke(this, name, id, lastId, glamour, jobId);
}
