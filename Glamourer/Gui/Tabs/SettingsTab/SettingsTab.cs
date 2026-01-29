using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Plugin.Services;
using Glamourer.Automation;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Gui.Tabs.DesignTab;
using Glamourer.Interop;
using Glamourer.Interop.PalettePlus;
using Glamourer.Services;
using ImSharp;
using Luna;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Text;
using OtterGui.Widgets;

namespace Glamourer.Gui.Tabs.SettingsTab;

public sealed class SettingsTab(
    Configuration config,
    DesignFileSystemSelector selector,
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
    PcpService pcpService)
    : ITab<MainTabType>
{
    private readonly VirtualKey[] _validKeys = keys.GetValidVirtualKeys().Prepend(VirtualKey.NO_KEY).ToArray();

    public ReadOnlySpan<byte> Label
        => "Settings"u8;

    public MainTabType Identifier
        => MainTabType.Settings;

    public void DrawContent()
    {
        using var child = ImUtf8.Child("MainWindowChild"u8, default);
        if (!child)
            return;

        Checkbox("Enable Auto Designs"u8,
            "Enable the application of designs associated to characters in the Automation tab to be applied automatically."u8,
            config.EnableAutoDesigns, v =>
            {
                config.EnableAutoDesigns = v;
                autoDesignApplier.OnEnableAutoDesignsChanged(v);
            });
        ImGui.NewLine();
        ImGui.NewLine();
        ImGui.NewLine();
        ImGui.NewLine();

        using (ImUtf8.Child("SettingsChild"u8, default))
        {
            DrawBehaviorSettings();
            DrawDesignDefaultSettings();
            DrawInterfaceSettings();
            DrawColorSettings();
            overrides.Draw();
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
        if (!ImUtf8.CollapsingHeader("Glamourer Behavior"u8))
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
        ImGui.NewLine();
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
        if (ImUtf8.ButtonEx("Delete all PCP Designs"u8, "Deletes all designs tagged with 'PCP' from the design list."u8, disabled: !active))
            pcpService.CleanPcpDesigns();
        if (!active)
            ImUtf8.HoverTooltip(ImGuiHoveredFlags.AllowWhenDisabled, $"\nHold {config.DeleteDesignModifier} while clicking.");
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
        if (!ImUtf8.CollapsingHeader("Design Defaults"))
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

        var tmp = config.PcpFolder;
        ImGui.SetNextItemWidth(0.4f * ImGui.GetContentRegionAvail().X);
        if (ImUtf8.InputText("##pcpFolder"u8, ref tmp))
            config.PcpFolder = tmp;

        if (ImGui.IsItemDeactivatedAfterEdit())
            config.Save();

        ImGuiUtil.LabeledHelpMarker("Default PCP Organizational Folder",
            "The folder any designs created due to penumbra character packs are moved to on creation.\nLeave blank to import into Root.");

        tmp = config.PcpColor;
        ImGui.SetNextItemWidth(0.4f * ImGui.GetContentRegionAvail().X);
        if (ImUtf8.InputText("##pcpColor"u8, ref tmp))
            config.PcpColor = tmp;

        if (ImGui.IsItemDeactivatedAfterEdit())
            config.Save();

        ImGuiUtil.LabeledHelpMarker("Default PCP Design Color",
            "The name of the color group any designs created due to penumbra character packs are assigned.\nLeave blank for no specific color assignment.");
    }

    private void DrawInterfaceSettings()
    {
        if (!ImUtf8.CollapsingHeader("Interface"u8))
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

        ImGui.Dummy(Vector2.Zero);
        ImGui.Separator();
        ImGui.Dummy(Vector2.Zero);

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
        ImGui.Dummy(Vector2.Zero);
        ImGui.Separator();
        ImGui.Dummy(Vector2.Zero);

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

        ImGui.NewLine();
        ImUtf8.Text("Show the following panels in their respective tabs:"u8);
        ImGui.Dummy(Vector2.Zero);
        DesignPanelFlagExtensions.DrawTable("##panelTable"u8, config.HideDesignPanel, config.AutoExpandDesignPanel, v =>
        {
            config.HideDesignPanel = v;
            config.Save();
        }, v =>
        {
            config.AutoExpandDesignPanel = v;
            config.Save();
        });


        ImGui.Dummy(Vector2.Zero);
        ImGui.Separator();
        ImGui.Dummy(Vector2.Zero);

        Checkbox("Allow Double-Clicking Designs to Apply"u8,
            "Tries to apply a design to the current player character When double-clicking it in the design selector."u8,
            config.AllowDoubleClickToApply, v => config.AllowDoubleClickToApply = v);
        Checkbox("Show all Application Rule Checkboxes for Automation"u8,
            "Show multiple separate application rule checkboxes for automated designs, instead of a single box for enabling or disabling."u8,
            config.ShowAllAutomatedApplicationRules, v => config.ShowAllAutomatedApplicationRules = v);
        Checkbox("Show Unobtained Item Warnings"u8,
            "Show information whether you have unlocked all items and customizations in your automated design or not."u8,
            config.ShowUnlockedItemWarnings, v => config.ShowUnlockedItemWarnings = v);
        Checkbox("Show Color Display Config"u8, "Show the Color Display configuration options in the Advanced Customization panels."u8,
            config.ShowColorConfig,             v => config.ShowColorConfig = v);
        Checkbox("Show Palette+ Import Button"u8,
            "Show the import button that allows you to import Palette+ palettes onto a design in the Advanced Customization options section for designs."u8,
            config.ShowPalettePlusImport, v => config.ShowPalettePlusImport = v);
        using (ImRaii.PushId(1))
        {
            PaletteImportButton();
        }

        Checkbox("Keep Advanced Dye Window Attached"u8,
            "Keeps the advanced dye window expansion attached to the main window, or makes it freely movable."u8,
            config.KeepAdvancedDyesAttached, v => config.KeepAdvancedDyesAttached = v);

        Checkbox("Debug Mode"u8, "Show the debug tab. Only useful for debugging or advanced use. Not recommended in general."u8,
            config.DebugMode,
            v => config.DebugMode = v);
        ImGui.NewLine();
    }

    private void DrawQuickDesignBoxes()
    {
        var showAuto   = config.EnableAutoDesigns;
        var numColumns = 9 - (showAuto ? 0 : 2) - (config.UseTemporarySettings ? 0 : 1);
        ImGui.NewLine();
        ImUtf8.Text("Show the Following Buttons in the Quick Design Bar:"u8);
        ImGui.Dummy(Vector2.Zero);
        using var table = ImUtf8.Table("##tableQdb"u8, numColumns,
            ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders | ImGuiTableFlags.NoHostExtendX);
        if (!table)
            return;

        ReadOnlySpan<(string, bool, QdbButtons)> columns =
        [
            ("Apply Design", true, QdbButtons.ApplyDesign),
            ("Revert All", true, QdbButtons.RevertAll),
            ("Revert to Auto", showAuto, QdbButtons.RevertAutomation),
            ("Reapply Auto", showAuto, QdbButtons.ReapplyAutomation),
            ("Revert Equip", true, QdbButtons.RevertEquip),
            ("Revert Customize", true, QdbButtons.RevertCustomize),
            ("Revert Advanced Customization", true, QdbButtons.RevertAdvancedCustomization),
            ("Revert Advanced Dyes", true, QdbButtons.RevertAdvancedDyes),
            ("Reset Settings", config.UseTemporarySettings, QdbButtons.ResetSettings),
        ];

        for (var i = 0; i < columns.Length; ++i)
        {
            if (!columns[i].Item2)
                continue;

            ImGui.TableNextColumn();
            ImUtf8.TableHeader(columns[i].Item1);
        }

        for (var i = 0; i < columns.Length; ++i)
        {
            if (!columns[i].Item2)
                continue;

            var       flag = columns[i].Item3;
            using var id   = ImUtf8.PushId((int)flag);
            ImGui.TableNextColumn();
            var offset = (ImGui.GetContentRegionAvail().X - Im.Style.FrameHeight) / 2;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
            var value = config.QdbButtons.HasFlag(flag);
            if (!ImUtf8.Checkbox(""u8, ref value))
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
        if (ImUtf8.Button("Import Palette+ to Designs"u8))
            paletteImport.ImportDesigns();
        ImUtf8.HoverTooltip(
            $"Import all existing Palettes from your Palette+ Config into Designs at PalettePlus/[Name] if these do not exist. Existing Palettes are:\n\n\t - {string.Join("\n\t - ", paletteImport.Data.Keys)}");
    }

    /// <summary> Draw the entire Color subsection. </summary>
    private void DrawColorSettings()
    {
        if (!ImUtf8.CollapsingHeader("Colors"u8))
            return;

        using (var tree = ImUtf8.TreeNode("Custom Design Colors"u8))
        {
            if (tree)
                designColorUi.Draw();
        }

        using (var tree = ImUtf8.TreeNode("Color Settings"u8))
        {
            if (tree)
                foreach (var color in Enum.GetValues<ColorId>())
                {
                    var (defaultColor, name, description) = color.Data();
                    var currentColor = config.Colors.GetValueOrDefault(color, defaultColor);
                    if (Widget.ColorPicker(name, description, currentColor, c => config.Colors[color] = c, defaultColor))
                        config.Save();
                }
        }

        ImGui.NewLine();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void Checkbox(ReadOnlySpan<byte> label, ReadOnlySpan<byte> tooltip, bool current, Action<bool> setter)
    {
        using var id  = ImUtf8.PushId(label);
        var       tmp = current;
        if (ImUtf8.Checkbox(""u8, ref tmp) && tmp != current)
        {
            setter(tmp);
            config.Save();
        }

        Im.Line.Same();
        ImUtf8.LabeledHelpMarker(label, tooltip);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void EphemeralCheckbox(ReadOnlySpan<byte> label, ReadOnlySpan<byte> tooltip, bool current, Action<bool> setter)
    {
        using var id  = ImUtf8.PushId(label);
        var       tmp = current;
        if (ImUtf8.Checkbox(""u8, ref tmp) && tmp != current)
        {
            setter(tmp);
            config.Ephemeral.Save();
        }

        Im.Line.Same();
        ImUtf8.LabeledHelpMarker(label, tooltip);
    }

    /// <summary> Different supported sort modes as a combo. </summary>
    private void DrawFolderSortType()
    {
        var sortMode = config.SortMode;
        ImGui.SetNextItemWidth(300 * Im.Style.GlobalScale);
        using (var combo = ImUtf8.Combo("##sortMode"u8, sortMode.Name))
        {
            if (combo)
                foreach (var val in Configuration.Constants.ValidSortModes)
                {
                    if (ImUtf8.Selectable(val.Name, val.GetType() == sortMode.GetType()) && val.GetType() != sortMode.GetType())
                    {
                        config.SortMode = val;
                        selector.SetFilterDirty();
                        config.Save();
                    }

                    ImUtf8.HoverTooltip(val.Description);
                }
        }

        ImUtf8.LabeledHelpMarker("Sort Mode"u8, "Choose the sort mode for the mod selector in the designs tab."u8);
    }

    private void DrawRenameSettings()
    {
        ImGui.SetNextItemWidth(300 * Im.Style.GlobalScale);
        using (var combo = ImUtf8.Combo("##renameSettings"u8, config.ShowRename.GetData().Name))
        {
            if (combo)
                foreach (var value in Enum.GetValues<RenameField>())
                {
                    var (name, desc) = value.GetData();
                    if (ImGui.Selectable(name, config.ShowRename == value))
                    {
                        config.ShowRename = value;
                        selector.SetRenameSearchPath(value);
                        config.Save();
                    }

                    ImUtf8.HoverTooltip(desc);
                }
        }

        Im.Line.Same();
        const string tt =
            "Select which of the two renaming input fields are visible when opening the right-click context menu of a design in the design selector.";
        ImGuiComponents.HelpMarker(tt);
        Im.Line.Same();
        ImUtf8.Text("Rename Fields in Design Context Menu"u8);
        ImUtf8.HoverTooltip(tt);
    }

    private void DrawHeightUnitSettings()
    {
        ImGui.SetNextItemWidth(300 * Im.Style.GlobalScale);
        using (var combo = ImUtf8.Combo("##heightUnit"u8, HeightDisplayTypeName(config.HeightDisplayType)))
        {
            if (combo)
                foreach (var type in Enum.GetValues<HeightDisplayType>())
                {
                    if (ImUtf8.Selectable(HeightDisplayTypeName(type), type == config.HeightDisplayType) && type != config.HeightDisplayType)
                    {
                        config.HeightDisplayType = type;
                        config.Save();
                    }
                }
        }

        Im.Line.Same();
        const string tt = "Select how to display the height of characters in real-world units, if at all.";
        ImGuiComponents.HelpMarker(tt);
        Im.Line.Same();
        ImUtf8.Text("Character Height Display Type"u8);
        ImUtf8.HoverTooltip(tt);
    }

    private static ReadOnlySpan<byte> HeightDisplayTypeName(HeightDisplayType type)
        => type switch
        {
            HeightDisplayType.None        => "Do Not Display"u8,
            HeightDisplayType.Centimetre  => "Centimetres (000.0 cm)"u8,
            HeightDisplayType.Metre       => "Metres (0.00 m)"u8,
            HeightDisplayType.Wrong       => "Inches (00.0 in)"u8,
            HeightDisplayType.WrongFoot   => "Feet (0'00'')"u8,
            HeightDisplayType.Corgi       => "Corgis (0.0 Corgis)"u8,
            HeightDisplayType.OlympicPool => "Olympic-size swimming Pools (0.000 Pools)"u8,
            _                             => ""u8,
        };
}
