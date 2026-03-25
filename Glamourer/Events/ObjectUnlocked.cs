using Luna;

namespace Glamourer.Events;

/// <summary> Triggered when a new item or customization is unlocked. </summary>
public sealed class ObjectUnlocked(Logger log)
    : EventBase<ObjectUnlocked.Arguments, ObjectUnlocked.Priority>(nameof(ObjectUnlocked), log), IRequiredService
{
    public enum Type
    {
        Item,
        Customization,
    }

    public enum Priority
    {
        /// <seealso cref="Gui.Tabs.UnlocksTab.UnlockTable.Cache.OnItemUnlock"/>
        /// <remarks> Currently used as a hack to make the unlock table dirty in it. If anything else starts using this, rework. </remarks>
        UnlockTable = 0,
    }


    /// <summary> Arguments for the ObjectUnlocked event. </summary>
    /// <param name="Type"> The type of the unlocked object. </param>
    /// <param name="Id"> The ID of the unlocked object. </param>
    /// <param name="Timestamp"> The timestamp of the unlock event.</param>
    public readonly record struct Arguments(Type Type, uint Id, DateTimeOffset Timestamp);
}
