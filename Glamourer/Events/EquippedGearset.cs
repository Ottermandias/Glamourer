using Luna;
using Penumbra.String;

namespace Glamourer.Events;

/// <summary> Triggered when the player equips a gear set. </summary>
public sealed class EquippedGearset(Logger log)
    : EventBase<EquippedGearset.Arguments, EquippedGearset.Priority>(nameof(EquippedGearset), log)
{
    public enum Priority
    {
        /// <seealso cref="Automation.AutoDesignApplier.OnEquippedGearset"/>
        AutoDesignApplier = 0,
    }

    /// <summary> Arguments for the EquippedGearset event. </summary>
    /// <param name="Name"> The name of the equipped gear set. </param>
    /// <param name="Id"> The ID of the equipped gear set.</param>
    /// <param name="PriorId"> The ID of the gear set previously equipped.</param>
    /// <param name="GlamourId"> The ID of the associated glamour plate. </param>
    /// <param name="JobId"> The job ID of the associated job. </param>
    public readonly record struct Arguments(ByteString Name, int Id, int PriorId, int GlamourId, byte JobId);
}
