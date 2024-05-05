using Glamourer.Designs;
using Glamourer.Gui;
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
public sealed class DesignChanged()
    : EventWrapper<DesignChanged.Type, Design, object?, DesignChanged.Priority>(nameof(DesignChanged))
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

        /// <summary> An existing design had its associated color changed. Data is the prior color [string]. </summary>
        ChangedColor,

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

        /// <summary> An existing design had a link to a different design added, removed or moved. Data is null. </summary>
        ChangedLink,

        /// <summary> An existing design had a customization changed. Data is the old value, the new value and the type [(CustomizeValue, CustomizeValue, CustomizeIndex)]. </summary>
        Customize,

        /// <summary> An existing design had its entire customize array changed. Data is the old array, the applied flags and the changed flags. [(CustomizeArray, CustomizeFlag, CustomizeFlag)]. </summary>
        EntireCustomize,

        /// <summary> An existing design had an equipment piece changed. Data is the old value, the new value and the slot [(EquipItem, EquipItem, EquipSlot)]. </summary>
        Equip,

        /// <summary> An existing design had its weapons changed. Data is the old mainhand, the old offhand, the new mainhand, the new offhand (if any) and the new gauntlets (if any). [(EquipItem, EquipItem, EquipItem, EquipItem?, EquipItem?)]. </summary>
        Weapon,

        /// <summary> An existing design had a stain changed. Data is the old stain id, the new stain id and the slot [(StainId, StainId, EquipSlot)]. </summary>
        Stain,

        /// <summary> An existing design had a crest visibility changed. Data is the old crest visibility, the new crest visibility and the slot [(bool, bool, EquipSlot)]. </summary>
        Crest,

        /// <summary> An existing design had a customize parameter changed. Data is the old value, the new value and the flag [(CustomizeParameterValue, CustomizeParameterValue, CustomizeParameterFlag)]. </summary>
        Parameter,

        /// <summary> An existing design had an advanced dye row added, changed, or deleted. Data is the old value, the new value and the index [(ColorRow?, ColorRow?, MaterialValueIndex)]. </summary>
        Material,

        /// <summary> An existing design had an advanced dye rows Revert state changed. Data is the index [MaterialValueIndex]. </summary>
        MaterialRevert,

        /// <summary> An existing design had changed whether it always forces a redraw or not. </summary>
        ForceRedraw,

        /// <summary> An existing design changed whether a specific customization is applied. Data is the type of customization [CustomizeIndex]. </summary>
        ApplyCustomize,

        /// <summary> An existing design changed whether a specific equipment piece is applied. Data is the slot of the equipment [EquipSlot]. </summary>
        ApplyEquip,

        /// <summary> An existing design changed whether a specific stain is applied. Data is the slot of the equipment [EquipSlot]. </summary>
        ApplyStain,

        /// <summary> An existing design changed whether a specific crest visibility is applied. Data is the slot of the equipment [EquipSlot]. </summary>
        ApplyCrest,

        /// <summary> An existing design changed whether a specific customize parameter is applied. Data is the flag for the parameter [CustomizeParameterFlag]. </summary>
        ApplyParameter,

        /// <summary> An existing design changed whether an advanced dye row is applied. Data is the index [MaterialValueIndex]. </summary>
        ApplyMaterial,

        /// <summary> An existing design changed its write protection status. Data is the new value [bool]. </summary>
        WriteProtection,

        /// <summary> An existing design changed its display status for the quick design bar. Data is the new value [bool]. </summary>
        QuickDesignBar,

        /// <summary> An existing design changed one of the meta flags. Data is the flag, whether it was about their applying and the new value [(MetaFlag, bool, bool)]. </summary>
        Other,
    }

    public enum Priority
    {
        /// <seealso cref="Designs.Links.DesignLinkManager.OnDesignChange"/>
        DesignLinkManager = 1,

        /// <seealso cref="Automation.AutoDesignManager.OnDesignChange"/>
        AutoDesignManager = 1,

        /// <seealso cref="DesignFileSystem.OnDesignChange"/>
        DesignFileSystem = 0,

        /// <seealso cref="Gui.Tabs.DesignTab.DesignFileSystemSelector.OnDesignChange"/>
        DesignFileSystemSelector = -1,

        /// <seealso cref="SpecialDesignCombo.OnDesignChange"/>
        DesignCombo = -2,
    }
}
