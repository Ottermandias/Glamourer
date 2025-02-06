using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using Glamourer.Automation;
using Glamourer.Designs;
using Glamourer.Gui.Tabs.DesignTab;
using Glamourer.Interop;
using Glamourer.Interop.PalettePlus;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Text;
using OtterGui.Widgets;

namespace Glamourer.Gui.Tabs.SettingsTab;

public class SettingsTab(
    Configuration config,
    DesignFileSystemSelector selector,
    ContextMenuService contextMenuService,
    IUiBuilder uiBuilder,
    GlamourerChangelog changelog,
    IKeyState keys,
    DesignColorUi designColorUi,
    PaletteImport paletteImport,
    PalettePlusChecker paletteChecker,
    CollectionOverrideDrawer overrides,
    CodeDrawer codeDrawer,
    Glamourer glamourer,
    AutoDesignApplier autoDesignApplier)
    : ITab
{
    private readonly VirtualKey[] _validKeys = keys.GetValidVirtualKeys().Prepend(VirtualKey.NO_KEY).ToArray();

    public ReadOnlySpan<byte> Label
        => "Settings"u8;

    public void DrawContent()
    {
        using var child = ImRaii.Child("MainWindowChild");
        if (!child)
            return;

        Checkbox("Enable Auto Designs",
            "Enable the application of designs associated to characters in the Automation tab to be applied automatically.",
            config.EnableAutoDesigns, v =>
            {
                config.EnableAutoDesigns = v;
                autoDesignApplier.OnEnableAutoDesignsChanged(v);
            });
        ImGui.NewLine();
        ImGui.NewLine();
        ImGui.NewLine();
        ImGui.NewLine();

        using (ImRaii.Child("SettingsChild"))
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

    private void DrawBehaviorSettings()
    {
        if (!ImGui.CollapsingHeader("Glamourer Behavior"))
            return;

        Checkbox("Always Apply Entire Weapon for Mainhand",
            "When manually applying a mainhand item, will also apply a corresponding offhand and potentially gauntlets for certain fist weapons.",
            config.ChangeEntireItem, v => config.ChangeEntireItem = v);
        Checkbox("Use Replacement Gear for Gear Unavailable to Your Race or Gender",
            "Use different gender- and race-appropriate models as a substitute when detecting certain items not available for a characters current gender and race.",
            config.UseRestrictedGearProtection, v => config.UseRestrictedGearProtection = v);
        Checkbox("Do Not Apply Unobtained Items in Automation",
            "Enable this if you want automatically applied designs to only consider items and customizations you have actually unlocked once, and skip those you have not.",
            config.UnlockedItemMode, v => config.UnlockedItemMode = v);
        Checkbox("Respect Manual Changes When Editing Automation",
            "Whether changing any currently active automation group will respect manual changes to the character before re-applying the changed automation or not.",
            config.RespectManualOnAutomationUpdate, v => config.RespectManualOnAutomationUpdate = v);
        Checkbox("Enable Festival Easter-Eggs",
            "Glamourer may do some fun things on specific dates. Disable this if you do not want your experience disrupted by this.",
            config.DisableFestivals == 0, v => config.DisableFestivals = v ? (byte)0 : (byte)2);
        Checkbox("Auto-Reload Gear",
            "Automatically reload equipment pieces on your own character when changing any mod options in Penumbra in their associated collection.",
            config.AutoRedrawEquipOnChanges, v => config.AutoRedrawEquipOnChanges = v);
        Checkbox("Revert Manual Changes on Zone Change",
            "Restores the old behaviour of reverting your character to its game or automation base whenever you change the zone.",
            config.RevertManualChangesOnZoneChange, v => config.RevertManualChangesOnZoneChange = v);
        Checkbox("Enable Advanced Customization Options",
            "Enable the display and editing of advanced customization options like arbitrary colors.",
            config.UseAdvancedParameters, paletteChecker.SetAdvancedParameters);
        PaletteImportButton();
        Checkbox("Enable Advanced Dye Options",
            "Enable the display and editing of advanced dyes (color sets) for all equipment",
            config.UseAdvancedDyes, v => config.UseAdvancedDyes = v);
        Checkbox("Always Apply Associated Mods",
            "Whenever a design is applied to a character (including via automation), Glamourer will try to apply its associated mod settings to the collection currently associated with that character, if it is available.\n\n"
          + "Glamourer will NOT revert these applied settings automatically. This may mess up your collection and configuration.\n\n"
          + "If you enable this setting, you are aware that any resulting misconfiguration is your own fault.",
            config.AlwaysApplyAssociatedMods, v => config.AlwaysApplyAssociatedMods = v);
        Checkbox("Use Temporary Mod Settings",
            "Apply all settings as temporary settings so they will be reset when Glamourer or the game shut down.", config.UseTemporarySettings,
            v => config.UseTemporarySettings = v);
        Checkbox("Prevent Random Design Repeats", 
            "When using random designs, prevent the same design from being chosen twice in a row.",
            config.PreventRandomRepeats, v => config.PreventRandomRepeats = v);
        ImGui.NewLine();
    }

    private void DrawDesignDefaultSettings()
    {
        if (!ImUtf8.CollapsingHeader("Design Defaults"))
            return;

        Checkbox("Show in Quick Design Bar", "Newly created designs will be shown in the quick design bar by default.",
            config.DefaultDesignSettings.ShowQuickDesignBar, v => config.DefaultDesignSettings.ShowQuickDesignBar = v);
        Checkbox("Reset Advanced Dyes", "Newly created designs will be configured to reset advanced dyes on application by default.",
            config.DefaultDesignSettings.ResetAdvancedDyes, v => config.DefaultDesignSettings.ResetAdvancedDyes = v);
        Checkbox("Always Force Redraw", "Newly created designs will be configured to force character redraws on application by default.",
            config.DefaultDesignSettings.AlwaysForceRedrawing, v => config.DefaultDesignSettings.AlwaysForceRedrawing = v);
        Checkbox("Reset Temporary Settings", "Newly created designs will be configured to clear all advanced settings applied by Glamourer to the collection by default.",
            config.DefaultDesignSettings.ResetTemporarySettings, v => config.DefaultDesignSettings.ResetTemporarySettings = v);
    }

    private void DrawInterfaceSettings()
    {
        if (!ImGui.CollapsingHeader("Interface"))
            return;

        EphemeralCheckbox("Show Quick Design Bar",
            "Show a bar separate from the main window that allows you to quickly apply designs or revert your character and target.",
            config.Ephemeral.ShowDesignQuickBar, v => config.Ephemeral.ShowDesignQuickBar = v);
        EphemeralCheckbox("Lock Quick Design Bar", "Prevent the quick design bar from being moved and lock it in place.",
            config.Ephemeral.LockDesignQuickBar,
            v => config.Ephemeral.LockDesignQuickBar = v);
        if (Widget.ModifiableKeySelector("Hotkey to Toggle Quick Design Bar", "Set a hotkey that opens or closes the quick design bar.",
                100 * ImGuiHelpers.GlobalScale,
                config.ToggleQuickDesignBar, v => config.ToggleQuickDesignBar = v, _validKeys))
            config.Save();
        Checkbox("Show Quick Design Bar in Main Window",
            "Show the quick design bar in the tab selection part of the main window, too.",
            config.ShowQuickBarInTabs, v => config.ShowQuickBarInTabs = v);
        DrawQuickDesignBoxes();

        ImGui.Dummy(Vector2.Zero);
        ImGui.Separator();
        ImGui.Dummy(Vector2.Zero);

        Checkbox("Enable Game Context Menus", "Whether to show a Try On via Glamourer button on context menus for equippable items.",
            config.EnableGameContextMenu,     v =>
            {
                config.EnableGameContextMenu = v;
                if (v)
                    contextMenuService.Enable();
                else
                    contextMenuService.Disable();
            });
        Checkbox("Show Window when UI is Hidden", "Whether to show Glamourer windows even when the games UI is hidden.",
            config.ShowWindowWhenUiHidden,        v =>
            {
                config.ShowWindowWhenUiHidden = v;
                uiBuilder.DisableUserUiHide   = v;
            });
        Checkbox("Hide Window in Cutscenes", "Whether the main Glamourer window should automatically be hidden when entering cutscenes or not.",
            config.HideWindowInCutscene,
            v =>
            {
                config.HideWindowInCutscene     = v;
                uiBuilder.DisableCutsceneUiHide = !v;
            });
        EphemeralCheckbox("Lock Main Window", "Prevent the main window from being moved and lock it in place.",
            config.Ephemeral.LockMainWindow,
            v => config.Ephemeral.LockMainWindow = v);
        Checkbox("Open Main Window at Game Start", "Whether the main Glamourer window should be open or closed after launching the game.",
            config.OpenWindowAtStart,              v => config.OpenWindowAtStart = v);
        ImGui.Dummy(Vector2.Zero);
        ImGui.Separator();
        ImGui.Dummy(Vector2.Zero);

        Checkbox("Smaller Equip Display", "Use single-line display without icons and small dye buttons instead of double-line display.",
            config.SmallEquip,            v => config.SmallEquip = v);
        DrawHeightUnitSettings();
        Checkbox("Show Application Checkboxes",
            "Show the application checkboxes in the Customization and Equipment panels of the design tab, instead of only showing them under Application Rules.",
            !config.HideApplyCheckmarks, v => config.HideApplyCheckmarks = !v);
        if (Widget.DoubleModifierSelector("Design Deletion Modifier",
                "A modifier you need to hold while clicking the Delete Design button for it to take effect.", 100 * ImGuiHelpers.GlobalScale,
                config.DeleteDesignModifier, v => config.DeleteDesignModifier = v))
            config.Save();
        DrawRenameSettings();
        Checkbox("Auto-Open Design Folders",
            "Have design folders open or closed as their default state after launching.", config.OpenFoldersByDefault,
            v => config.OpenFoldersByDefault = v);
        DrawFolderSortType();

        ImGui.Dummy(Vector2.Zero);
        ImGui.Separator();
        ImGui.Dummy(Vector2.Zero);

        Checkbox("Allow Double-Clicking Designs to Apply",
            "Tries to apply a design to the current player character When double-clicking it in the design selector.",
            config.AllowDoubleClickToApply, v => config.AllowDoubleClickToApply = v);
        Checkbox("Show all Application Rule Checkboxes for Automation",
            "Show multiple separate application rule checkboxes for automated designs, instead of a single box for enabling or disabling.",
            config.ShowAllAutomatedApplicationRules, v => config.ShowAllAutomatedApplicationRules = v);
        Checkbox("Show Unobtained Item Warnings",
            "Show information whether you have unlocked all items and customizations in your automated design or not.",
            config.ShowUnlockedItemWarnings, v => config.ShowUnlockedItemWarnings = v);
        if (config.UseAdvancedParameters)
        {
            Checkbox("Show Color Display Config", "Show the Color Display configuration options in the Advanced Customization panels.",
                config.ShowColorConfig,           v => config.ShowColorConfig = v);
            Checkbox("Show Palette+ Import Button",
                "Show the import button that allows you to import Palette+ palettes onto a design in the Advanced Customization options section for designs.",
                config.ShowPalettePlusImport, v => config.ShowPalettePlusImport = v);
            using var id = ImRaii.PushId(1);
            PaletteImportButton();
        }

        if (config.UseAdvancedDyes)
            Checkbox("Keep Advanced Dye Window Attached",
                "Keeps the advanced dye window expansion attached to the main window, or makes it freely movable.",
                config.KeepAdvancedDyesAttached, v => config.KeepAdvancedDyesAttached = v);

        Checkbox("Debug Mode", "Show the debug tab. Only useful for debugging or advanced use. Not recommended in general.", config.DebugMode,
            v => config.DebugMode = v);
        ImGui.NewLine();
    }

    private void DrawQuickDesignBoxes()
    {
        var showAuto     = config.EnableAutoDesigns;
        var showAdvanced = config.UseAdvancedParameters || config.UseAdvancedDyes;
        var numColumns   = 7 - (showAuto ? 0 : 2) - (showAdvanced ? 0 : 1);
        ImGui.NewLine();
        ImGui.TextUnformatted("Show the Following Buttons in the Quick Design Bar:");
        ImGui.Dummy(Vector2.Zero);
        using var table = ImRaii.Table("##tableQdb", numColumns,
            ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders | ImGuiTableFlags.NoHostExtendX);
        if (!table)
            return;

        var columns = new[]
        {
            (" Apply Design ", true, QdbButtons.ApplyDesign),
            (" Revert All ", true, QdbButtons.RevertAll),
            (" Revert to Auto ", showAuto, QdbButtons.RevertAutomation),
            (" Reapply Auto ", showAuto, QdbButtons.ReapplyAutomation),
            (" Revert Equip ", true, QdbButtons.RevertEquip),
            (" Revert Customize ", true, QdbButtons.RevertCustomize),
            (" Revert Advanced ", showAdvanced, QdbButtons.RevertAdvanced),
        };

        foreach (var (label, _, _) in columns.Where(t => t.Item2))
        {
            ImGui.TableNextColumn();
            ImGui.TableHeader(label);
        }

        foreach (var (_, _, flag) in columns.Where(t => t.Item2))
        {
            using var id = ImRaii.PushId((int)flag);
            ImGui.TableNextColumn();
            var offset = (ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight()) / 2;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
            var value = config.QdbButtons.HasFlag(flag);
            if (!ImGui.Checkbox(string.Empty, ref value))
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
        if (!config.UseAdvancedParameters || !config.ShowPalettePlusImport)
            return;

        ImGui.SameLine();
        if (ImGui.Button("Import Palette+ to Designs"))
            paletteImport.ImportDesigns();
        ImGuiUtil.HoverTooltip(
            $"Import all existing Palettes from your Palette+ Config into Designs at PalettePlus/[Name] if these do not exist. Existing Palettes are:\n\n\t - {string.Join("\n\t - ", paletteImport.Data.Keys)}");
    }

    /// <summary> Draw the entire Color subsection. </summary>
    private void DrawColorSettings()
    {
        if (!ImGui.CollapsingHeader("Colors"))
            return;

        using (var tree = ImRaii.TreeNode("Custom Design Colors"))
        {
            if (tree)
                designColorUi.Draw();
        }

        using (var tree = ImRaii.TreeNode("Color Settings"))
        {
            if (tree)
                foreach (var color in Enum.GetValues<ColorId>())
                {
                    var (defaultColor, name, description) = color.Data();
                    var currentColor = config.Colors.TryGetValue(color, out var current) ? current : defaultColor;
                    if (Widget.ColorPicker(name, description, currentColor, c => config.Colors[color] = c, defaultColor))
                        config.Save();
                }
        }

        ImGui.NewLine();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void Checkbox(string label, string tooltip, bool current, Action<bool> setter)
    {
        using var id  = ImRaii.PushId(label);
        var       tmp = current;
        if (ImGui.Checkbox(string.Empty, ref tmp) && tmp != current)
        {
            setter(tmp);
            config.Save();
        }

        ImGui.SameLine();
        ImGuiUtil.LabeledHelpMarker(label, tooltip);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void EphemeralCheckbox(string label, string tooltip, bool current, Action<bool> setter)
    {
        using var id  = ImRaii.PushId(label);
        var       tmp = current;
        if (ImGui.Checkbox(string.Empty, ref tmp) && tmp != current)
        {
            setter(tmp);
            config.Ephemeral.Save();
        }

        ImGui.SameLine();
        ImGuiUtil.LabeledHelpMarker(label, tooltip);
    }

    /// <summary> Different supported sort modes as a combo. </summary>
    private void DrawFolderSortType()
    {
        var sortMode = config.SortMode;
        ImGui.SetNextItemWidth(300 * ImGuiHelpers.GlobalScale);
        using (var combo = ImRaii.Combo("##sortMode", sortMode.Name))
        {
            if (combo)
                foreach (var val in Configuration.Constants.ValidSortModes)
                {
                    if (ImGui.Selectable(val.Name, val.GetType() == sortMode.GetType()) && val.GetType() != sortMode.GetType())
                    {
                        config.SortMode = val;
                        selector.SetFilterDirty();
                        config.Save();
                    }

                    ImGuiUtil.HoverTooltip(val.Description);
                }
        }

        ImGuiUtil.LabeledHelpMarker("Sort Mode", "Choose the sort mode for the mod selector in the designs tab.");
    }

    private void DrawRenameSettings()
    {
        ImGui.SetNextItemWidth(300 * ImGuiHelpers.GlobalScale);
        using (var combo = ImRaii.Combo("##renameSettings", config.ShowRename.GetData().Name))
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

                    ImGuiUtil.HoverTooltip(desc);
                }
        }

        ImGui.SameLine();
        const string tt =
            "Select which of the two renaming input fields are visible when opening the right-click context menu of a design in the design selector.";
        ImGuiComponents.HelpMarker(tt);
        ImGui.SameLine();
        ImGui.TextUnformatted("Rename Fields in Design Context Menu");
        ImGuiUtil.HoverTooltip(tt);
    }

    private void DrawHeightUnitSettings()
    {
        ImGui.SetNextItemWidth(300 * ImGuiHelpers.GlobalScale);
        using (var combo = ImRaii.Combo("##heightUnit", HeightDisplayTypeName(config.HeightDisplayType)))
        {
            if (combo)
                foreach (var type in Enum.GetValues<HeightDisplayType>())
                {
                    if (ImGui.Selectable(HeightDisplayTypeName(type), type == config.HeightDisplayType) && type != config.HeightDisplayType)
                    {
                        config.HeightDisplayType = type;
                        config.Save();
                    }
                }
        }

        ImGui.SameLine();
        const string tt = "Select how to display the height of characters in real-world units, if at all.";
        ImGuiComponents.HelpMarker(tt);
        ImGui.SameLine();
        ImGui.TextUnformatted("Character Height Display Type");
        ImGuiUtil.HoverTooltip(tt);
    }

    private static string HeightDisplayTypeName(HeightDisplayType type)
        => type switch
        {
            HeightDisplayType.None        => "Do Not Display",
            HeightDisplayType.Centimetre  => "Centimetres (000.0 cm)",
            HeightDisplayType.Metre       => "Metres (0.00 m)",
            HeightDisplayType.Wrong       => "Inches (00.0 in)",
            HeightDisplayType.WrongFoot   => "Feet (0'00'')",
            HeightDisplayType.Corgi       => "Corgis (0.0 Corgis)",
            HeightDisplayType.OlympicPool => "Olympic-size swimming Pools (0.000 Pools)",
            _                             => string.Empty,
        };
}
