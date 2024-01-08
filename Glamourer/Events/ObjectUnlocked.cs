using OtterGui.Classes;

namespace Glamourer.Events;

/// <summary>
/// Triggered when a new item or customization is unlocked.
/// <list type="number">
///     <item>Parameter is the type of the unlocked object </item>
///     <item>Parameter is the id of the unlocked object. </item>
///     <item>Parameter is the timestamp of the unlock. </item>
/// </list>
/// </summary>
public sealed class ObjectUnlocked()
    : EventWrapper<ObjectUnlocked.Type, uint, DateTimeOffset, ObjectUnlocked.Priority>(nameof(ObjectUnlocked))
{
    public enum Type
    {
        Item,
        Customization,
    }

    public enum Priority
    {
        /// <seealso cref="Gui.Tabs.UnlocksTab.UnlockTable.OnObjectUnlock"/>
        /// <remarks> Currently used as a hack to make the unlock table dirty in it. If anything else starts using this, rework. </remarks>
        UnlockTable = 0,
    }
}
