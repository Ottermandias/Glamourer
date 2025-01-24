using Glamourer.Automation;
using OtterGui.Classes;

namespace Glamourer.Events;

/// <summary>
/// Triggered when an automated design is changed in any way.
/// <list type="number">
///     <item>Parameter is the type of the change </item>
///     <item>Parameter is the added or changed design set or null on deletion. </item>
///     <item>Parameter is additional data depending on the type of change. </item>
/// </list>
/// </summary>
public sealed class AutomationChanged()
    : EventWrapper<AutomationChanged.Type, AutoDesignSet?, object?, AutomationChanged.Priority>(nameof(AutomationChanged))
{
    public enum Type
    {
        /// <summary> Add a new set. Names and identifiers do not have to be unique. It is not enabled by default. Additional data is the index it gets added at and the name [(int, string)]. </summary>
        AddedSet,

        /// <summary> Delete a given set. Additional data is the index it got removed from [int].</summary>
        DeletedSet,

        /// <summary> Rename a given set. Names do not have to be unique. Additional data is the old name and the new name [(string, string)]. </summary>
        RenamedSet,

        /// <summary> Move a given set to a different position. Additional data is the old index of the set and the new index of the set [(int, int)]. </summary>
        MovedSet,

        /// <summary> Change the identifier a given set is associated with to another one. Additional data is the old identifier and the new one, and a potentially disabled other design set. [(ActorIdentifier[], ActorIdentifier, AutoDesignSet?)]. </summary>
        ChangeIdentifier,

        /// <summary> Toggle the enabled state of a given set. Additional data is the thus disabled other set, if any [AutoDesignSet?]. </summary>
        ToggleSet,

        /// <summary> Change the used base state of a given set. Additional data is prior and new base. [(AutoDesignSet.Base, AutoDesignSet.Base)]. </summary>
        ChangedBase,

        /// <summary> Change the resetting of temporary settings for a given set. Additional data is the new value.  </summary>
        ChangedTemporarySettingsReset,

        /// <summary> Add a new associated design to a given set. Additional data is the index it got added at [int]. </summary>
        AddedDesign,

        /// <summary> Remove a given associated design from a given set. Additional data is the index it got removed from [int]. </summary>
        DeletedDesign,

        /// <summary> Move a given associated design in the list of a given set. Additional data is the index that got moved and the index it got moved to [(int, int)]. </summary>
        MovedDesign,

        /// <summary> Change the linked design in an associated design for a given set. Additional data is the index of the changed associated design, the old linked design and the new linked design [(int, IDesignStandIn, IDesignStandIn)]. </summary>
        ChangedDesign,

        /// <summary> Change the job condition in an associated design for a given set. Additional data is the index of the changed associated design, the old job group and the new job group [(int, JobGroup, JobGroup)]. </summary>
        ChangedConditions,

        /// <summary> Change the application type in an associated design for a given set. Additional data is the index of the changed associated design, the old type and the new type. [(int, AutoDesign.Type, AutoDesign.Type)]. </summary>
        ChangedType,

        /// <summary> Change the additional data for a specific design type. Additional data is the index of the changed associated design and the new data. [(int, object)] </summary>
        ChangedData,
    }

    public enum Priority
    {
        /// <seealso cref="Gui.Tabs.AutomationTab.SetSelector.OnAutomationChange"/>
        SetSelector = 0,

        /// <seealso cref="AutoDesignApplier.OnAutomationChange"/>
        AutoDesignApplier = 0,

        /// <seealso cref="Gui.Tabs.AutomationTab.RandomRestrictionDrawer.OnAutomationChange"/>
        RandomRestrictionDrawer = -1,
    }
}
