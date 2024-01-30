using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using Glamourer.Designs;
using Glamourer.Gui.Tabs.DesignTab;
using Glamourer.Interop;
using Glamourer.Interop.PalettePlus;
using Glamourer.Interop.Penumbra;
using Glamourer.Services;
using Glamourer.State;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Widgets;

namespace Glamourer.Gui.Tabs;

public class SettingsTab(
    Configuration config,
    DesignFileSystemSelector selector,
    CodeService codeService,
    PenumbraAutoRedraw autoRedraw,
    ContextMenuService contextMenuService,
    UiBuilder uiBuilder,
    GlamourerChangelog changelog,
    FunModule funModule,
    IKeyState keys,
    DesignColorUi designColorUi,
    PaletteImport paletteImport,
    PalettePlusChecker paletteChecker)
    : ITab
{
    private readonly VirtualKey[] _validKeys = keys.GetValidVirtualKeys().Prepend(VirtualKey.NO_KEY).ToArray();

    public ReadOnlySpan<byte> Label
        => "Settings"u8;

    private string _currentCode = string.Empty;

    public void DrawContent()
    {
        using var child = ImRaii.Child("MainWindowChild");
        if (!child)
            return;

        Checkbox("Enable Auto Designs",
            "Enable the application of designs associated to characters in the Automation tab to be applied automatically.",
            config.EnableAutoDesigns, v => config.EnableAutoDesigns = v);
        ImGui.NewLine();
        ImGui.NewLine();
        ImGui.NewLine();

        using (var child2 = ImRaii.Child("SettingsChild"))
        {
            DrawBehaviorSettings();
            DrawInterfaceSettings();
            DrawColorSettings();
            DrawCodes();
        }

        MainWindow.DrawSupportButtons(changelog.Changelog);
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
        Checkbox("Enable Festival Easter-Eggs",
            "Glamourer may do some fun things on specific dates. Disable this if you do not want your experience disrupted by this.",
            config.DisableFestivals == 0, v => config.DisableFestivals = v ? (byte)0 : (byte)2);
        Checkbox("Auto-Reload Gear",
            "Automatically reload equipment pieces on your own character when changing any mod options in Penumbra in their associated collection.",
            config.AutoRedrawEquipOnChanges, autoRedraw.SetState);
        Checkbox("Revert Manual Changes on Zone Change",
            "Restores the old behaviour of reverting your character to its game or automation base whenever you change the zone.",
            config.RevertManualChangesOnZoneChange, v => config.RevertManualChangesOnZoneChange = v);
        Checkbox("Enable Advanced Customization Options",
            "Enable the display and editing of advanced customization options like arbitrary colors.",
            config.UseAdvancedParameters, paletteChecker.SetAdvancedParameters);
        Checkbox("Always Apply Associated Mods",
            "Whenever a design is applied to a character (including via automation), Glamourer will try to apply its associated mod settings to the collection currently associated with that character, if it is available.\n\n"
          + "Glamourer will NOT revert these applied settings automatically. This may mess up your collection and configuration.\n\n"
          + "If you enable this setting, you are aware that any resulting misconfiguration is your own fault.",
            config.AlwaysApplyAssociatedMods, v => config.AlwaysApplyAssociatedMods = v);
        PaletteImportButton();
        ImGui.NewLine();
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
        Checkbox("Show Application Checkboxes",
            "Show the application checkboxes in the Customization and Equipment panels of the design tab, instead of only showing them under Application Rules.",
            !config.HideApplyCheckmarks, v => config.HideApplyCheckmarks = !v);
        if (Widget.DoubleModifierSelector("Design Deletion Modifier",
                "A modifier you need to hold while clicking the Delete Design button for it to take effect.", 100 * ImGuiHelpers.GlobalScale,
                config.DeleteDesignModifier, v => config.DeleteDesignModifier = v))
            config.Save();
        Checkbox("Auto-Open Design Folders",
            "Have design folders open or closed as their default state after launching.", config.OpenFoldersByDefault,
            v => config.OpenFoldersByDefault = v);
        DrawFolderSortType();

        ImGui.Dummy(Vector2.Zero);
        ImGui.Separator();
        ImGui.Dummy(Vector2.Zero);

        Checkbox("Show all Application Rule Checkboxes for Automation",
            "Show multiple separate application rule checkboxes for automated designs, instead of a single box for enabling or disabling.",
            config.ShowAllAutomatedApplicationRules, v => config.ShowAllAutomatedApplicationRules = v);
        Checkbox("Show Unobtained Item Warnings",
            "Show information whether you have unlocked all items and customizations in your automated design or not.",
            config.ShowUnlockedItemWarnings, v => config.ShowUnlockedItemWarnings = v);
        if (config.UseAdvancedParameters)
        {
            Checkbox("Show Revert Advanced Customizations Button in Quick Design Bar",
                "Show a button to revert only advanced customizations on your character or a target in the quick design bar.",
                config.ShowRevertAdvancedParametersButton, v => config.ShowRevertAdvancedParametersButton = v);
            Checkbox("Show Color Display Config", "Show the Color Display configuration options in the Advanced Customization panels.",
                config.ShowColorConfig,           v => config.ShowColorConfig = v);
            Checkbox("Show Palette+ Import Button",
                "Show the import button that allows you to import Palette+ palettes onto a design in the Advanced Customization options section for designs.",
                config.ShowPalettePlusImport, v => config.ShowPalettePlusImport = v);
            using var id = ImRaii.PushId(1);
            PaletteImportButton();
        }

        Checkbox("Debug Mode", "Show the debug tab. Only useful for debugging or advanced use. Not recommended in general.", config.DebugMode,
            v => config.DebugMode = v);
        ImGui.NewLine();
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

    private void DrawCodes()
    {
        const string tooltip =
            "Cheat Codes are not actually for cheating in the game, but for 'cheating' in Glamourer. They allow for some fun easter-egg modes that usually manipulate the appearance of all players you see (including yourself) in some way.\n\n"
          + "Cheat Codes are generally pop culture references, but it is unlikely you will be able to guess any of them based on nothing. Some codes have been published on the discord server, but other than that, we are still undecided on how and when to publish them or add any new ones. Maybe some will be hidden in the change logs or on the help pages. Or maybe I will just add hints in this section later on.\n\n"
          + "In any case, you are not losing out on anything important if you never look at this section and there is no real reason to go on a treasure hunt for them. It is mostly something I added because it was fun for me.";

        var show = ImGui.CollapsingHeader("Cheat Codes");
        if (ImGui.IsItemHovered())
        {
            ImGui.SetNextWindowSize(new Vector2(400, 0));
            using var tt = ImRaii.Tooltip();
            ImGuiUtil.TextWrapped(tooltip);
        }

        if (!show)
            return;

        using (var style = ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, ImGuiHelpers.GlobalScale, _currentCode.Length > 0))
        {
            var       color = codeService.CheckCode(_currentCode) != null ? ColorId.ActorAvailable : ColorId.ActorUnavailable;
            using var c     = ImRaii.PushColor(ImGuiCol.Border, color.Value(), _currentCode.Length > 0);
            if (ImGui.InputTextWithHint("##Code", "Enter Cheat Code...", ref _currentCode, 512, ImGuiInputTextFlags.EnterReturnsTrue))
                if (codeService.AddCode(_currentCode))
                    _currentCode = string.Empty;
        }

        ImGui.SameLine();
        ImGuiComponents.HelpMarker(tooltip);

        DrawCodeHints();

        if (config.Codes.Count <= 0)
            return;

        for (var i = 0; i < config.Codes.Count; ++i)
        {
            var (code, state) = config.Codes[i];
            var action = codeService.CheckCode(code);
            if (action == null)
                continue;

            if (ImGui.Checkbox(code, ref state))
            {
                action(state);
                codeService.SaveState();
            }
        }

        if (ImGui.Button("Who am I?!?"))
            funModule.WhoAmI();

        ImGui.SameLine();

        if (ImGui.Button("Who is that!?!"))
            funModule.WhoIsThat();
    }

    private void DrawCodeHints()
    {
        // TODO
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
}
