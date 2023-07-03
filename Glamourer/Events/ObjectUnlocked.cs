using System;
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
public sealed class ObjectUnlocked : EventWrapper<Action<ObjectUnlocked.Type, uint, DateTimeOffset>, ObjectUnlocked.Priority>
{
    public enum Type
    {
        Item,
        Customization,
    }

    public enum Priority
    {
        /// <seealso cref="Gui.Tabs.UnlocksTab.UnlockTable.OnObjectUnlock"/>
        UnlockTable = 0,
    }

    public ObjectUnlocked()
        : base(nameof(ObjectUnlocked))
    { }

    public void Invoke(Type type, uint id, DateTimeOffset timestamp)
        => Invoke(this, type, id, timestamp);
}
