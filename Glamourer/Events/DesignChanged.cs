using System;
using Glamourer.Designs;
using OtterGui.Classes;

namespace Glamourer.Events;

/// <summary>
/// Triggered when a Design is edited in any way.
/// <list type="number">
///     <item>Parameter is the type of the change </item>
///     <item>Parameter is the changed Design. </item>
///     <item>Parameter is any additional data depending on the type of change. </item>
/// </list>
/// </summary>
public sealed class DesignChanged : EventWrapper<Action<DesignChanged.Type, Design, object?>, DesignChanged.Priority>
{
    public enum Type
    {
        /// <summary> A new design was created. Data is a potential path to move it to [string?]. </summary>
        Created,

        /// <summary> An existing design was deleted. Data is null. </summary>
        Deleted,

        /// <summary> Invoked on full reload. Design and Data are null. </summary>
        ReloadedAll,

        /// <summary> An existing design was renamed. Data is the prior name [string]. </summary>
        Renamed,

        /// <summary> An existing design had its description changed. Data is the prior description [string]. </summary>
        ChangedDescription,

        /// <summary> An existing design had a new tag added. Data is the new tag and the index it was added at [(string, int)]. </summary>
        AddedTag,

        /// <summary> An existing design had an existing tag removed. Data is the removed tag and the index it had before removal [(string, int)]. </summary>
        RemovedTag,

        /// <summary> An existing design had an existing tag renamed. Data is the old name of the tag, the new name of the tag, and the index it had before being resorted [(string, string, int)]. </summary>
        ChangedTag,

        /// <summary> An existing design had a new associated mod added. Data is the Mod and its Settings [(Mod, ModSettings)]. </summary>
        AddedMod,

        /// <summary> An existing design had an existing associated mod removed. Data is the Mod and its Settings [(Mod, ModSettings)]. </summary>
        RemovedMod,

        /// <summary> An existing design had a customization changed. Data is the old value, the new value and the type [(CustomizeValue, CustomizeValue, CustomizeIndex)]. </summary>
        Customize,

        /// <summary> An existing design had an equipment piece changed. Data is the old value, the new value and the slot [(EquipItem, EquipItem, EquipSlot)]. </summary>
        Equip,

        /// <summary> An existing design had its weapons changed. Data is the old mainhand, the old offhand, the new mainhand and the new offhand [(EquipItem, EquipItem, EquipItem, EquipItem)]. </summary>
        Weapon,

        /// <summary> An existing design had a stain changed. Data is the old stain id, the new stain id and the slot [(StainId, StainId, EquipSlot)]. </summary>
        Stain,

        /// <summary> An existing design changed whether a specific customization is applied. Data is the type of customization [CustomizeIndex]. </summary>
        ApplyCustomize,

        /// <summary> An existing design changed whether a specific equipment is applied. Data is the slot of the equipment [EquipSlot]. </summary>
        ApplyEquip,

        /// <summary> An existing design changed whether a specific stain is applied. Data is the slot of the equipment [EquipSlot]. </summary>
        ApplyStain,

        /// <summary> An existing design changed its write protection status. Data is the new value [bool]. </summary>
        WriteProtection,

        /// <summary> An existing design changed one of the meta flags. Data is the flag, whether it was about their applying and the new value [(MetaFlag, bool, bool)]. </summary>
        Other,
    }

    public enum Priority
    {
        /// <seealso cref="DesignFileSystem.OnDesignChange"/>
        DesignFileSystem = 0,

        /// <seealso cref="Gui.Tabs.DesignTab.DesignFileSystemSelector.OnDesignChange"/>
        DesignFileSystemSelector = -1,

        /// <seealso cref="Automation.AutoDesignManager.OnDesignChange"/>
        AutoDesignManager = 1,
    }

    public DesignChanged()
        : base(nameof(DesignChanged))
    { }

    public void Invoke(Type type, Design design, object? data = null)
        => Invoke(this, type, design, data);
}
