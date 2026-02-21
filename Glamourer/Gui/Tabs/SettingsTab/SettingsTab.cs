using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using Dalamud.Plugin.Services;
using Glamourer.Automation;
using Glamourer.Config;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Gui.Tabs.DesignTab;
using Glamourer.Interop;
using Glamourer.Interop.PalettePlus;
using Glamourer.Services;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.SettingsTab;

public sealed class SettingsTab(
    Configuration config,
    DesignFileSystemDrawer drawer,
    ContextMenuService contextMenuService,
    IUiBuilder uiBuilder,
    GlamourerChangelog changelog,
    IKeyState keys,
    DesignColorUi designColorUi,
    PaletteImport paletteImport,
    CollectionOverrideDrawer overrides,
    CodeDrawer codeDrawer,
    Glamourer glamourer,
    AutoDesignApplier autoDesignApplier,
    AutoRedrawChanged autoRedraw,
    PcpService pcpService,
    IgnoredMods ignoredMods)
    : ITab<MainTabType>
{
    private readonly VirtualKey[] _validKeys = keys.GetValidVirtualKeys().Prepend(VirtualKey.NO_KEY).ToArray();

    public ReadOnlySpan<byte> Label
        => "Settings"u8;

    public MainTabType Identifier
        => MainTabType.Settings;

    public void DrawContent()
    {
        using var child = Im.Child.Begin("MainWindowChild"u8);
        if (!child)
            return;

        Checkbox("Enable Auto Designs"u8,
            "Enable the application of designs associated to characters in the Automation tab to be applied automatically."u8,
            config.EnableAutoDesigns, v =>
            {
                config.EnableAutoDesigns = v;
                autoDesignApplier.OnEnableAutoDesignsChanged(v);
            });
        Im.Cursor.Y += Im.Style.FrameHeightWithSpacing * 4;

        using (Im.Child.Begin("SettingsChild"u8))
        {
            DrawBehaviorSettings();
            DrawDesignDefaultSettings();
            DrawInterfaceSettings();
            DrawColorSettings();
            overrides.Draw();
            DrawIgnoredMods();
            codeDrawer.Draw();
        }

        MainWindow.DrawSupportButtons(glamourer, changelog.Changelog);
    }

    public void DrawPenumbraIntegrationSettings()
    {
        DrawPenumbraIntegrationSettings1();
        DrawPenumbraIntegrationSettings2();
    }

    private void DrawBehaviorSettings()
    {
        if (!Im.Tree.Header("Glamourer Behavior"u8))
            return;

        Checkbox("Always Apply Entire Weapon for Mainhand"u8,
            "When manually applying a mainhand item, will also apply a corresponding offhand and potentially gauntlets for certain fist weapons."u8,
            config.ChangeEntireItem, v => config.ChangeEntireItem = v);
        Checkbox("Use Replacement Gear for Gear Unavailable to Your Race or Gender"u8,
            "Use different gender- and race-appropriate models as a substitute when detecting certain items not available for a characters current gender and race."u8,
            config.UseRestrictedGearProtection, v => config.UseRestrictedGearProtection = v);
        Checkbox("Do Not Apply Unobtained Items in Automation"u8,
            "Enable this if you want automatically applied designs to only consider items and customizations you have actually unlocked once, and skip those you have not."u8,
            config.UnlockedItemMode, v => config.UnlockedItemMode = v);
        Checkbox("Respect Manual Changes When Editing Automation"u8,
            "Whether changing any currently active automation group will respect manual changes to the character before re-applying the changed automation or not."u8,
            config.RespectManualOnAutomationUpdate, v => config.RespectManualOnAutomationUpdate = v);
        Checkbox("Enable Festival Easter-Eggs"u8,
            "Glamourer may do some fun things on specific dates. Disable this if you do not want your experience disrupted by this."u8,
            config.DisableFestivals == 0, v => config.DisableFestivals = v ? (byte)0 : (byte)2);
        DrawPenumbraIntegrationSettings1();
        Checkbox("Revert Manual Changes on Zone Change"u8,
            "Restores the old behaviour of reverting your character to its game or automation base whenever you change the zone."u8,
            config.RevertManualChangesOnZoneChange, v => config.RevertManualChangesOnZoneChange = v);
        PaletteImportButton();
        DrawPenumbraIntegrationSettings2();
        Checkbox("Prevent Random Design Repeats"u8,
            "When using random designs, prevent the same design from being chosen twice in a row."u8,
            config.PreventRandomRepeats, v => config.PreventRandomRepeats = v);
        Im.Line.New();
    }

    private void DrawPenumbraIntegrationSettings1()
    {
        Checkbox("Auto-Reload Gear"u8,
            "Automatically reload equipment pieces on your own character when changing any mod options in Penumbra in their associated collection."u8,
            config.AutoRedrawEquipOnChanges, v =>
            {
                config.AutoRedrawEquipOnChanges = v;
                autoRedraw.Invoke(v);
            });
        Checkbox("Attach to PCP Handling"u8,
            "Add the actor's glamourer state when a PCP is created by Penumbra, and create a design and apply it if possible when a PCP is installed by Penumbra."u8,
            config.AttachToPcp, pcpService.Set);
        var active = config.DeleteDesignModifier.IsActive();
        Im.Line.Same();
        if (ImEx.Button("Delete all PCP Designs"u8, default, "Deletes all designs tagged with 'PCP' from the design list."u8, !active))
            pcpService.CleanPcpDesigns();
        if (!active)
            Im.Tooltip.OnHover(HoveredFlags.AllowWhenDisabled, $"\nHold {config.DeleteDesignModifier} while clicking.");
    }

    private void DrawPenumbraIntegrationSettings2()
    {
        Checkbox("Always Apply Associated Mods"u8,
            "Whenever a design is applied to a character (including via automation), Glamourer will try to apply its associated mod settings to the collection currently associated with that character, if it is available.\n\n"u8
          + "Glamourer will NOT revert these applied settings automatically. This may mess up your collection and configuration.\n\n"u8
          + "If you enable this setting, you are aware that any resulting misconfiguration is your own fault."u8,
            config.AlwaysApplyAssociatedMods, v => config.AlwaysApplyAssociatedMods = v);
        Checkbox("Use Temporary Mod Settings"u8,
            "Apply all settings as temporary settings so they will be reset when Glamourer or the game shut down."u8,
            config.UseTemporarySettings,
            v => config.UseTemporarySettings = v);
    }

    private void DrawDesignDefaultSettings()
    {
        if (!Im.Tree.Header("Design Defaults"u8))
            return;

        Checkbox("Locked Designs"u8, "Newly created designs will be locked to prevent unintended changes."u8,
            config.DefaultDesignSettings.Locked, v => config.DefaultDesignSettings.Locked = v);
        Checkbox("Show in Quick Design Bar"u8, "Newly created designs will be shown in the quick design bar by default."u8,
            config.DefaultDesignSettings.ShowQuickDesignBar, v => config.DefaultDesignSettings.ShowQuickDesignBar = v);
        Checkbox("Reset Advanced Dyes"u8, "Newly created designs will be configured to reset advanced dyes on application by default."u8,
            config.DefaultDesignSettings.ResetAdvancedDyes, v => config.DefaultDesignSettings.ResetAdvancedDyes = v);
        Checkbox("Always Force Redraw"u8, "Newly created designs will be configured to force character redraws on application by default."u8,
            config.DefaultDesignSettings.AlwaysForceRedrawing, v => config.DefaultDesignSettings.AlwaysForceRedrawing = v);
        Checkbox("Reset Temporary Settings"u8,
            "Newly created designs will be configured to clear all advanced settings applied by Glamourer to the collection by default."u8,
            config.DefaultDesignSettings.ResetTemporarySettings, v => config.DefaultDesignSettings.ResetTemporarySettings = v);

        Im.Item.SetNextWidth(0.4f * Im.ContentRegion.Available.X);
        if (ImEx.InputOnDeactivation.Text("##pcpFolder"u8, config.PcpFolder, out string newPcpFolder))
        {
            config.PcpFolder = newPcpFolder;
            config.Save();
        }

        LunaStyle.DrawAlignedHelpMarkerLabel("Default PCP Organizational Folder"u8,
            "The folder any designs created due to penumbra character packs are moved to on creation.\nLeave blank to import into Root."u8);

        Im.Item.SetNextWidth(0.4f * Im.ContentRegion.Available.X);
        if (ImEx.InputOnDeactivation.Text("##pcpColor"u8, config.PcpColor, out string newPcpColor))
        {
            config.PcpColor = newPcpColor;
            config.Save();
        }

        LunaStyle.DrawAlignedHelpMarkerLabel("Default PCP Design Color"u8,
            "The name of the color group any designs created due to penumbra character packs are assigned.\nLeave blank for no specific color assignment."u8);
    }

    private void DrawInterfaceSettings()
    {
        if (!Im.Tree.Header("Interface"u8))
            return;

        EphemeralCheckbox("Show Quick Design Bar"u8,
            "Show a bar separate from the main window that allows you to quickly apply designs or revert your character and target."u8,
            config.Ephemeral.ShowDesignQuickBar, v => config.Ephemeral.ShowDesignQuickBar = v);
        EphemeralCheckbox("Lock Quick Design Bar"u8, "Prevent the quick design bar from being moved and lock it in place."u8,
            config.Ephemeral.LockDesignQuickBar,
            v => config.Ephemeral.LockDesignQuickBar = v);
        if (KeySelector.ModifiableKeySelector("Hotkey to Toggle Quick Design Bar"u8,
                "Set a hotkey that opens or closes the quick design bar."u8,
                100 * Im.Style.GlobalScale, config.ToggleQuickDesignBar, v => config.ToggleQuickDesignBar = v, _validKeys))
            config.Save();

        Checkbox("Show Quick Design Bar in Main Window"u8,
            "Show the quick design bar in the tab selection part of the main window, too."u8,
            config.ShowQuickBarInTabs, v => config.ShowQuickBarInTabs = v);
        DrawQuickDesignBoxes();

        Im.Dummy(Vector2.Zero);
        Im.Separator();
        Im.Dummy(Vector2.Zero);

        Checkbox("Enable Game Context Menus"u8, "Whether to show a Try On via Glamourer button on context menus for equippable items."u8,
            config.EnableGameContextMenu,       v =>
            {
                config.EnableGameContextMenu = v;
                if (v)
                    contextMenuService.Enable();
                else
                    contextMenuService.Disable();
            });
        Checkbox("Show Window when UI is Hidden"u8, "Whether to show Glamourer windows even when the games UI is hidden."u8,
            config.ShowWindowWhenUiHidden,          v =>
            {
                config.ShowWindowWhenUiHidden = v;
                uiBuilder.DisableUserUiHide   = v;
            });
        Checkbox("Hide Window in Cutscenes"u8,
            "Whether the main Glamourer window should automatically be hidden when entering cutscenes or not."u8,
            config.HideWindowInCutscene,
            v =>
            {
                config.HideWindowInCutscene     = v;
                uiBuilder.DisableCutsceneUiHide = !v;
            });
        EphemeralCheckbox("Lock Main Window"u8, "Prevent the main window from being moved and lock it in place."u8,
            config.Ephemeral.LockMainWindow,
            v => config.Ephemeral.LockMainWindow = v);
        Checkbox("Open Main Window at Game Start"u8, "Whether the main Glamourer window should be open or closed after launching the game."u8,
            config.OpenWindowAtStart,                v => config.OpenWindowAtStart = v);
        Im.Dummy(Vector2.Zero);
        Im.Separator();
        Im.Dummy(Vector2.Zero);

        Checkbox("Smaller Equip Display"u8, "Use single-line display without icons and small dye buttons instead of double-line display."u8,
            config.SmallEquip,              v => config.SmallEquip = v);
        DrawHeightUnitSettings();
        Checkbox("Show Application Checkboxes"u8,
            "Show the application checkboxes in the Customization and Equipment panels of the design tab, instead of only showing them under Application Rules."u8,
            !config.HideApplyCheckmarks, v => config.HideApplyCheckmarks = !v);
        if (KeySelector.DoubleModifier("Design Deletion Modifier"u8,
                "A modifier you need to hold while clicking the Delete Design button for it to take effect."u8, 100 * Im.Style.GlobalScale,
                config.DeleteDesignModifier, v => config.DeleteDesignModifier = v))
            config.Save();
        if (KeySelector.DoubleModifier("Incognito Modifier"u8,
                "A modifier you need to hold while clicking the Incognito button for it to take effect."u8, 100 * Im.Style.GlobalScale,
                config.IncognitoModifier, v => config.IncognitoModifier = v))
            config.Save();
        DrawRenameSettings();
        Checkbox("Auto-Open Design Folders"u8,
            "Have design folders open or closed as their default state after launching."u8, config.OpenFoldersByDefault,
            v => config.OpenFoldersByDefault = v);
        DrawFolderSortType();

        Im.Line.New();
        Im.Text("Show the following panels in their respective tabs:"u8);
        Im.Dummy(Vector2.Zero);
        DesignPanelFlagExtensions.DrawTable("##panelTable"u8, config.HideDesignPanel, config.AutoExpandDesignPanel, v =>
        {
            config.HideDesignPanel = v;
            config.Save();
        }, v =>
        {
            config.AutoExpandDesignPanel = v;
            config.Save();
        });


        Im.Dummy(Vector2.Zero);
        Im.Separator();
        Im.Dummy(Vector2.Zero);

        Checkbox("Allow Double-Clicking Designs to Apply"u8,
            "Tries to apply a design to the current player character When double-clicking it in the design selector."u8,
            config.AllowDoubleClickToApply, v => config.AllowDoubleClickToApply = v);
        Checkbox("Show all Application Rule Checkboxes for Automation"u8,
            "Show multiple separate application rule checkboxes for automated designs, instead of a single box for enabling or disabling."u8,
            config.ShowAllAutomatedApplicationRules, v => config.ShowAllAutomatedApplicationRules = v);
        Checkbox("Show Unobtained Item Warnings"u8,
            "Show information whether you have unlocked all items and customizations in your automated design or not."u8,
            config.ShowUnlockedItemWarnings, v => config.ShowUnlockedItemWarnings = v);
        Checkbox("Show Color Display Configuration"u8, "Show the Color Display configuration options in the Advanced Customization panels."u8,
            config.ShowColorConfig,                    v => config.ShowColorConfig = v);
        Checkbox("Show Palette+ Import Button"u8,
            "Show the import button that allows you to import Palette+ palettes onto a design in the Advanced Customization options section for designs."u8,
            config.ShowPalettePlusImport, v => config.ShowPalettePlusImport = v);
        using (Im.Id.Push(1))
        {
            PaletteImportButton();
        }

        Checkbox("Keep Advanced Dye Window Attached"u8,
            "Keeps the advanced dye window expansion attached to the main window, or makes it freely movable."u8,
            config.KeepAdvancedDyesAttached, v => config.KeepAdvancedDyesAttached = v);

        Checkbox("Debug Mode"u8, "Show the debug tab. Only useful for debugging or advanced use. Not recommended in general."u8,
            config.DebugMode,
            v => config.DebugMode = v);
        Im.Line.New();
    }

    private readonly (StringU8, QdbButtons)[] _columns =
    [
        (new StringU8("Toggle Main Window"u8), QdbButtons.ToggleMainWindow),
        (new StringU8("Apply Design"u8), QdbButtons.ApplyDesign),
        (new StringU8("Revert All"u8), QdbButtons.RevertAll),
        (new StringU8("Revert to Auto"u8), QdbButtons.RevertAutomation),
        (new StringU8("Reapply Auto"u8), QdbButtons.ReapplyAutomation),
        (new StringU8("Revert Equip"u8), QdbButtons.RevertEquip),
        (new StringU8("Revert Customize"u8), QdbButtons.RevertCustomize),
        (new StringU8("Revert Advanced Customization"u8), QdbButtons.RevertAdvancedCustomization),
        (new StringU8("Revert Advanced Dyes"u8), QdbButtons.RevertAdvancedDyes),
        (new StringU8("Reset Settings"u8), QdbButtons.ResetSettings),
    ];

    private static bool DisplayButton(QdbButtons button, bool showAuto, bool useTemporarySettings)
        => button switch
        {
            QdbButtons.RevertAutomation  => showAuto,
            QdbButtons.ReapplyAutomation => showAuto,
            QdbButtons.ResetSettings     => useTemporarySettings,
            _                            => true,
        };

    private void DrawQuickDesignBoxes()
    {
        var showAuto   = config.EnableAutoDesigns;
        var numColumns = 10 - (showAuto ? 0 : 2) - (config.UseTemporarySettings ? 0 : 1);
        Im.Line.New();
        Im.Text("Show the Following Buttons in the Quick Design Bar:"u8);
        Im.Dummy(Vector2.Zero);
        using var table = Im.Table.Begin("##tableQdb"u8, numColumns, TableFlags.SizingFixedFit | TableFlags.Borders | TableFlags.NoHostExtendX);
        if (!table)
            return;


        // ReSharper disable once PossibleMultipleEnumeration
        foreach (var (text, flag) in _columns)
        {
            if (!DisplayButton(flag, showAuto, config.UseTemporarySettings))
                continue;

            table.NextColumn();
            table.Header(text);
        }

        // ReSharper disable once PossibleMultipleEnumeration
        foreach (var (_, flag) in _columns)
        {
            if (!DisplayButton(flag, showAuto, config.UseTemporarySettings))
                continue;

            using var id = Im.Id.Push((int)flag);
            table.NextColumn();
            var offset = (Im.ContentRegion.Available.X - Im.Style.FrameHeight) / 2;
            Im.Cursor.X += offset;
            var value = config.QdbButtons.HasFlag(flag);
            if (!Im.Checkbox(""u8, ref value))
                continue;

            var buttons = value ? config.QdbButtons | flag : config.QdbButtons & ~flag;
            if (buttons == config.QdbButtons)
                continue;

            config.QdbButtons = buttons;
            config.Save();
        }
    }

    private void PaletteImportButton()
    {
        if (!config.ShowPalettePlusImport)
            return;

        Im.Line.Same();
        if (Im.Button("Import Palette+ to Designs"u8))
            paletteImport.ImportDesigns();
        Im.Tooltip.OnHover(
            $"Import all existing Palettes from your Palette+ Configuration into Designs at PalettePlus/[Name] if these do not exist. Existing Palettes are:\n\n\t - {string.Join("\n\t - ", paletteImport.Data.Keys)}");
    }

    /// <summary> Draw the entire Color subsection. </summary>
    private void DrawColorSettings()
    {
        if (!Im.Tree.Header("Colors"u8))
            return;

        using (var tree = Im.Tree.Node("Custom Design Colors"u8))
        {
            if (tree)
                designColorUi.Draw();
        }

        using (var tree = Im.Tree.Node("Color Settings"u8))
        {
            if (tree)
                foreach (var color in ColorId.Values)
                {
                    var (defaultColor, name, description) = color.Data();
                    var currentColor = config.Colors.GetValueOrDefault(color, defaultColor);
                    if (!ImEx.ColorPicker(name, description, currentColor, out var newColor, defaultColor))
                        continue;

                    config.Colors[color] = newColor.Color;
                    CacheManager.Instance.SetColorsDirty();
                    config.Save();
                }
        }

        Im.Line.New();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void Checkbox(ReadOnlySpan<byte> label, ReadOnlySpan<byte> tooltip, bool current, Action<bool> setter)
    {
        using var id  = Im.Id.Push(label);
        var       tmp = current;
        if (Im.Checkbox(""u8, ref tmp) && tmp != current)
        {
            setter(tmp);
            config.Save();
        }

        LunaStyle.DrawAlignedHelpMarkerLabel(label, tooltip);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void EphemeralCheckbox(ReadOnlySpan<byte> label, ReadOnlySpan<byte> tooltip, bool current, Action<bool> setter)
    {
        using var id  = Im.Id.Push(label);
        var       tmp = current;
        if (Im.Checkbox(""u8, ref tmp) && tmp != current)
        {
            setter(tmp);
            config.Ephemeral.Save();
        }

        LunaStyle.DrawAlignedHelpMarkerLabel(label, tooltip);
    }

    /// <summary> Different supported sort modes as a combo. </summary>
    private void DrawFolderSortType()
    {
        var sortMode = config.SortMode;
        Im.Item.SetNextWidthScaled(300);
        using (var combo = Im.Combo.Begin("##sortMode"u8, sortMode.Name))
        {
            if (combo)
                foreach (var (_, value) in ISortMode.Valid)
                {
                    if (Im.Selectable(value.Name, value.GetType() == sortMode.GetType()) && value.GetType() != sortMode.GetType())
                    {
                        config.SortMode = value;
                        drawer.SortMode = value;
                        config.Save();
                    }

                    Im.Tooltip.OnHover(value.Description);
                }
        }

        LunaStyle.DrawAlignedHelpMarkerLabel("Sort Mode"u8, "Choose the sort mode for the design selector in the designs tab."u8);
    }

    private void DrawRenameSettings()
    {
        Im.Item.SetNextWidthScaled(300);
        using (var combo = Im.Combo.Begin("##renameSettings"u8, config.ShowRename.ToNameU8()))
        {
            if (combo)
                foreach (var value in RenameField.Values)
                {
                    if (Im.Selectable(value.ToNameU8(), config.ShowRename == value))
                    {
                        config.ShowRename = value;
                        config.Save();
                    }

                    Im.Tooltip.OnHover(value.Tooltip());
                }
        }

        LunaStyle.DrawAlignedHelpMarkerLabel("Rename Fields in Design Context Menu"u8,
            "Select which of the two renaming input fields are visible when opening the right-click context menu of a design in the design selector."u8);
    }

    private void DrawHeightUnitSettings()
    {
        Im.Item.SetNextWidthScaled(300);
        using (var combo = Im.Combo.Begin("##heightUnit"u8, config.HeightDisplayType.Tooltip()))
        {
            if (combo)
                foreach (var type in HeightDisplayType.Values)
                {
                    if (Im.Selectable(type.Tooltip(), type == config.HeightDisplayType) && type != config.HeightDisplayType)
                    {
                        config.HeightDisplayType = type;
                        config.Save();
                    }
                }
        }

        LunaStyle.DrawAlignedHelpMarkerLabel("Character Height Display Type"u8,
            "Select how to display the height of characters in real-world units, if at all."u8);
    }

    private string _newIgnoredMod = string.Empty;

    private void DrawIgnoredMods()
    {
        using var header = Im.Tree.HeaderId("Ignored Mods"u8);
        Im.Tooltip.OnHover("Add mods that are ignored for the 'modded' column in the Unlocks tab."u8);
        if (!header)
            return;

        using var listBox = Im.ListBox.Begin("##box"u8, new Vector2(0.4f * Im.ContentRegion.Available.X, Im.Style.FrameHeightWithSpacing * 10));
        if (!listBox)
            return;

        var       delete    = string.Empty;
        using var alignment = ImStyleDouble.ButtonTextAlign.PushX(0);
        foreach (var (idx, mod) in ignoredMods.Index())
        {
            using var id = Im.Id.Push(idx);
            if (ImEx.Icon.Button(LunaStyle.DeleteIcon, "Delete this ignored mod."u8))
                delete = mod;

            Im.Line.SameInner();
            ImEx.TextFramed(mod, Im.ContentRegion.Available with { Y = Im.Style.FrameHeight});
        }

        if (delete.Length > 0)
            ignoredMods.Remove(delete);

        var tt = _newIgnoredMod.Length is 0       ? "Please enter a new mod name or mod directory to ignore."u8 :
            ignoredMods.Contains(_newIgnoredMod) ? "This mod is already ignored."u8 :
                                                    "Ignore all mods with this name or directory in the Unlocks tab."u8;
        if (ImEx.Icon.Button(LunaStyle.AddObjectIcon, tt, tt[0] is not (byte)'I'))
        {
            ignoredMods.Add(_newIgnoredMod);
            _newIgnoredMod = string.Empty;
        }

        Im.Line.SameInner();
        Im.Item.SetNextWidthFull();
        Im.Input.Text("##newMod"u8, ref _newIgnoredMod, "Ignore this Mod..."u8);
    }
}
