using Glamourer.Automation;
using Glamourer.Designs;
using Luna;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Structs;

namespace Glamourer.Events;

/// <summary> Triggered when an automated design is changed in any way. </summary>
public sealed class AutomationChanged(Logger log)
    : EventBase<AutomationChanged.Arguments, AutomationChanged.Priority>(nameof(AutomationChanged), log)
{
    public enum Type
    {
        /// <summary> Add a new set. Names and identifiers do not have to be unique. It is not enabled by default. </summary>
        AddedSet,

        /// <summary> Delete a given set. </summary>
        DeletedSet,

        /// <summary> Rename a given set. Names do not have to be unique. </summary>
        RenamedSet,

        /// <summary> Move a given set to a different position. </summary>
        MovedSet,

        /// <summary> Change the identifier a given set is associated with to another one. </summary>
        ChangeIdentifier,

        /// <summary> Toggle the enabled state of a given set. </summary>
        ToggleSet,

        /// <summary> Change the used base state of a given set. </summary>
        ChangedBase,

        /// <summary> Change the resetting of temporary settings for a given set. </summary>
        ChangedTemporarySettingsReset,

        /// <summary> Add a new associated design to a given set. </summary>
        AddedDesign,

        /// <summary> Remove a given associated design from a given set. </summary>
        DeletedDesign,

        /// <summary> Move a given associated design in the list of a given set. </summary>
        MovedDesign,

        /// <summary> Change the linked design in an associated design for a given set. </summary>
        ChangedDesign,

        /// <summary> Change the job condition in an associated design for a given set. </summary>
        ChangedConditions,

        /// <summary> Change the application type in an associated design for a given set. </summary>
        ChangedType,

        /// <summary> Change the additional data for a specific design type. </summary>
        ChangedData,
    }

    public enum Priority
    {
        /// <seealso cref="Gui.Tabs.AutomationTab.AutomationSelection.OnAutomationChanged"/>
        AutomationSelection = 0,

        /// <seealso cref="Gui.Tabs.AutomationTab.SetSelector.Cache.OnAutomationChanged"/>
        SetSelector = 0,

        /// <seealso cref="AutoDesignApplier.OnAutomationChange"/>
        AutoDesignApplier = 0,

        /// <seealso cref="Gui.Tabs.AutomationTab.RandomRestrictionDrawer.OnAutomationChange"/>
        RandomRestrictionDrawer = -1,
    }

    /// <param name="Type"> The type of change. </param>
    public record Arguments(Type Type, AutoDesignSet Set)
    {
        public T As<T>() where T : Arguments
            => (T)this;
    }

    /// <param name="Set"> The added set. </param>
    /// <param name="Index"> The index the set was added at. </param>
    /// <param name="Name"> The name of the added set. </param>
    public sealed record AddedSetArguments(AutoDesignSet Set, int Index, string Name) : Arguments(Type.AddedSet, Set);

    /// <param name="Set"> The deleted set. </param>
    /// <param name="Index"> The index the set was deleted from. </param>
    public sealed record DeletedSetArguments(AutoDesignSet Set, int Index) : Arguments(Type.DeletedSet, Set);

    /// <param name="Set"> The renamed set. </param>
    /// <param name="OldName"> The old name of the set. </param>
    /// <param name="NewName"> The new name of the set. </param>
    public sealed record RenamedSetArguments(AutoDesignSet Set, string OldName, string NewName) : Arguments(Type.RenamedSet, Set);

    /// <param name="Set"> The moved set. </param>
    /// <param name="OldIndex"> The index the set was moved from. </param>
    /// <param name="NewIndex"> The index the set was moved to. </param>
    public sealed record MovedSetArguments(AutoDesignSet Set, int OldIndex, int NewIndex) : Arguments(Type.MovedSet, Set);

    /// <param name="Set"> The set that got its associated identifiers changed. </param>
    /// <param name="OldIdentifiers"> The prior identifiers that got removed. </param>
    /// <param name="NewIdentifier"> The new identifiers associated with the set. </param>
    /// <param name="DisabledSet"> A set that was associated with the new identifiers before and got disabled through this change, or null. </param>
    public sealed record ChangeIdentifierArguments(
        AutoDesignSet Set,
        ActorIdentifier[] OldIdentifiers,
        ActorIdentifier NewIdentifier,
        AutoDesignSet? DisabledSet) : Arguments(Type.ChangeIdentifier, Set);

    /// <param name="Set"> The set that got toggled on or off. </param>
    /// <param name="DisabledSet"> A set with the same association that got disabled due to this change, or null. </param>
    public sealed record ToggleSetArguments(AutoDesignSet Set, AutoDesignSet? DisabledSet) : Arguments(Type.ToggleSet, Set);

    /// <param name="Set"> The set that changed its base state. </param>
    /// <param name="OldBase"> The old base state of the set. </param>
    /// <param name="NewBase"> The new base state of the set. </param>
    public sealed record ChangedBaseArguments(AutoDesignSet Set, AutoDesignSet.Base OldBase, AutoDesignSet.Base NewBase) : Arguments(Type.ChangedBase, Set);

    /// <param name="Set"> The set that changed whether it resets all temporary settings. </param>
    /// <param name="NewValue"> The new state of resetting temporary settings. </param>
    public sealed record ChangedTemporarySettingsResetArguments(AutoDesignSet Set, bool NewValue) : Arguments(Type.ChangedTemporarySettingsReset, Set);

    /// <param name="Set"> The set that added a new design. </param>
    /// <param name="Index"> The index the new design was added in the set. </param>
    public sealed record AddedDesignArguments(AutoDesignSet Set, int Index) : Arguments(Type.AddedDesign, Set);

    /// <param name="Set"> The set that removed a design. </param>
    /// <param name="Index"> The index the design was removed from. </param>
    public sealed record DeletedDesignArguments(AutoDesignSet Set, int Index) : Arguments(Type.DeletedDesign, Set);

    /// <param name="Set"> The set that moved a design. </param>
    /// <param name="OldIndex"> The index the design was moved from in the set. </param>
    /// <param name="NewIndex"> The index the design was moved from to the set. </param>
    public sealed record MovedDesignArguments(AutoDesignSet Set, int OldIndex, int NewIndex) : Arguments(Type.MovedDesign, Set);

    /// <param name="Set"> The set that changed a design. </param>
    /// <param name="DesignIndex"> The index of the changed design. </param>
    /// <param name="OldDesign"> The design previously assigned to the set. </param>
    /// <param name="NewDesign"> The design the old one was changed to. </param>
    public sealed record ChangedDesignArguments(AutoDesignSet Set, int DesignIndex, IDesignStandIn OldDesign, IDesignStandIn NewDesign)
        : Arguments(Type.ChangedDesign, Set);

    /// <param name="Set"> The set that changed a job group condition. </param>
    /// <param name="DesignIndex"> The index of the changed design. </param>
    /// <param name="OldGroup"> The prior job condition for the design. </param>
    /// <param name="NewGroup"> The new job condition for the design. </param>
    public sealed record ChangedConditionsArguments(AutoDesignSet Set, int DesignIndex, JobGroup OldGroup, JobGroup NewGroup)
        : Arguments(Type.ChangedConditions, Set);

    /// <param name="Set"> The set that changed its application type. </param>
    /// <param name="DesignIndex"> The index of the changed design. </param>
    /// <param name="OldType"> The old application flags. </param>
    /// <param name="NewType"> The new application flags. </param>
    public sealed record ChangedTypeArguments(AutoDesignSet Set, int DesignIndex, ApplicationType OldType, ApplicationType NewType)
        : Arguments(Type.ChangedType, Set);

    /// <param name="Set"> The set that got a design data changed. </param>
    /// <param name="DesignIndex"> The index of the changed design. </param>
    /// <param name="NewData"> The new additional data for the changed design. </param>
    public sealed record ChangedDataArguments(AutoDesignSet Set, int DesignIndex, object NewData) : Arguments(Type.ChangedData, Set);
}
