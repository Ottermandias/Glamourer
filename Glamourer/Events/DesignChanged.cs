using Glamourer.Designs;
using Glamourer.Designs.History;
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
    : EventWrapper<DesignChanged.Type, Design, ITransaction?, DesignChanged.Priority>(nameof(DesignChanged))
{
    public enum Type
    {
        /// <summary> A new design was created. </summary>
        Created,

        /// <summary> An existing design was deleted. </summary>
        Deleted,

        /// <summary> Invoked on full reload. </summary>
        ReloadedAll,

        /// <summary> An existing design was renamed. </summary>
        Renamed,

        /// <summary> An existing design had its description changed. </summary>
        ChangedDescription,

        /// <summary> An existing design had its associated color changed. </summary>
        ChangedColor,

        /// <summary> An existing design had a new tag added. </summary>
        AddedTag,

        /// <summary> An existing design had an existing tag removed. </summary>
        RemovedTag,

        /// <summary> An existing design had an existing tag renamed. </summary>
        ChangedTag,

        /// <summary> An existing design had a new associated mod added. </summary>
        AddedMod,

        /// <summary> An existing design had an existing associated mod removed. </summary>
        RemovedMod,

        /// <summary> An existing design had an existing associated mod updated. </summary>
        UpdatedMod,

        /// <summary> An existing design had a link to a different design added, removed or moved. </summary>
        ChangedLink,

        /// <summary> An existing design had a customization changed. </summary>
        Customize,

        /// <summary> An existing design had its entire customize array changed. </summary>
        EntireCustomize,

        /// <summary> An existing design had an equipment piece changed. </summary>
        Equip,

        /// <summary> An existing design had a bonus item changed. </summary>
        BonusItem,

        /// <summary> An existing design had its weapons changed. </summary>
        Weapon,

        /// <summary> An existing design had a stain changed. </summary>
        Stains,

        /// <summary> An existing design had a crest visibility changed. </summary>
        Crest,

        /// <summary> An existing design had a customize parameter changed. </summary>
        Parameter,

        /// <summary> An existing design had an advanced dye row added, changed, or deleted. </summary>
        Material,

        /// <summary> An existing design had an advanced dye rows Revert state changed. </summary>
        MaterialRevert,

        /// <summary> An existing design had changed whether it always forces a redraw or not. </summary>
        ForceRedraw,

        /// <summary> An existing design had changed whether it always resets advanced dyes or not. </summary>
        ResetAdvancedDyes,

        /// <summary> An existing design had changed whether it always resets all prior temporary settings or not. </summary>
        ResetTemporarySettings,

        /// <summary> An existing design changed whether a specific customization is applied. </summary>
        ApplyCustomize,

        /// <summary> An existing design changed whether a specific equipment piece is applied. </summary>
        ApplyEquip,

        /// <summary> An existing design changed whether a specific bonus item is applied. </summary>
        ApplyBonusItem,

        /// <summary> An existing design changed whether a specific stain is applied. </summary>
        ApplyStain,

        /// <summary> An existing design changed whether a specific crest visibility is applied. </summary>
        ApplyCrest,

        /// <summary> An existing design changed whether a specific customize parameter is applied. </summary>
        ApplyParameter,

        /// <summary> An existing design changed whether an advanced dye row is applied. </summary>
        ApplyMaterial,

        /// <summary> An existing design changed its write protection status. </summary>
        WriteProtection,

        /// <summary> An existing design changed its display status for the quick design bar. </summary>
        QuickDesignBar,

        /// <summary> An existing design changed one of the meta flags. </summary>
        Other,
    }

    public enum Priority
    {
        /// <seealso cref="Designs.Links.DesignLinkManager.OnDesignChanged"/>
        DesignLinkManager = 1,

        /// <seealso cref="Automation.AutoDesignManager.OnDesignChange"/>
        AutoDesignManager = 1,

        /// <seealso cref="DesignFileSystem.OnDesignChange"/>
        DesignFileSystem = 0,

        /// <seealso cref="Gui.Tabs.DesignTab.DesignFileSystemSelector.OnDesignChange"/>
        DesignFileSystemSelector = -1,

        /// <seealso cref="DesignComboBase.OnDesignChanged"/>
        DesignCombo = -2,

        /// <seealso cref="EditorHistory.OnDesignChanged" />
        EditorHistory = -1000,
    }
}
