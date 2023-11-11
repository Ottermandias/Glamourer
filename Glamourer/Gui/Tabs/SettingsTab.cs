using System;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using Glamourer.Gui.Tabs.DesignTab;
using Glamourer.Interop;
using Glamourer.Interop.Penumbra;
using Glamourer.Services;
using Glamourer.State;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Widgets;

namespace Glamourer.Gui.Tabs;

public class SettingsTab : ITab
{
    private readonly VirtualKey[]             _validKeys;
    private readonly Configuration            _config;
    private readonly DesignFileSystemSelector _selector;
    private readonly CodeService              _codeService;
    private readonly PenumbraAutoRedraw       _autoRedraw;
    private readonly ContextMenuService       _contextMenuService;
    private readonly UiBuilder                _uiBuilder;
    private readonly GlamourerChangelog       _changelog;
    private readonly FunModule                _funModule;

    public SettingsTab(Configuration config, DesignFileSystemSelector selector, CodeService codeService, PenumbraAutoRedraw autoRedraw,
        ContextMenuService contextMenuService, UiBuilder uiBuilder, GlamourerChangelog changelog, FunModule funModule, IKeyState keys)
    {
        _config             = config;
        _selector           = selector;
        _codeService        = codeService;
        _autoRedraw         = autoRedraw;
        _contextMenuService = contextMenuService;
        _uiBuilder          = uiBuilder;
        _changelog          = changelog;
        _funModule          = funModule;
        _validKeys          = keys.GetValidVirtualKeys().Prepend(VirtualKey.NO_KEY).ToArray();
    }

    public ReadOnlySpan<byte> Label
        => "Settings"u8;

    private string _currentCode = string.Empty;

    public void DrawContent()
    {
        using var child = ImRaii.Child("MainWindowChild");
        if (!child)
            return;

        Checkbox("Enable Auto Designs", "Enable the application of designs associated to characters to be applied automatically.",
            _config.EnableAutoDesigns,  v => _config.EnableAutoDesigns = v);
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

        MainWindow.DrawSupportButtons(_changelog.Changelog);
    }

    private void DrawBehaviorSettings()
    {
        if (!ImGui.CollapsingHeader("Glamourer Behavior"))
            return;

        Checkbox("Use Replacement Gear for Gear Unavailable to Your Race or Gender",
            "Use different gender- and race-appropriate models as a substitute when detecting certain items not available for a characters current gender and race.",
            _config.UseRestrictedGearProtection, v => _config.UseRestrictedGearProtection = v);
        Checkbox("Do Not Apply Unobtained Items in Automation",
            "Enable this if you want automatically applied designs to only consider items and customizations you have actually unlocked once, and skip those you have not.",
            _config.UnlockedItemMode, v => _config.UnlockedItemMode = v);
        Checkbox("Enable Festival Easter-Eggs",
            "Glamourer may do some fun things on specific dates. Disable this if you do not want your experience disrupted by this.",
            _config.DisableFestivals == 0, v => _config.DisableFestivals = v ? (byte)0 : (byte)2);
        Checkbox("Auto-Reload Gear",
            "Automatically reload equipment pieces on your own character when changing any mod options in Penumbra in their associated collection.",
            _config.AutoRedrawEquipOnChanges, _autoRedraw.SetState);
        Checkbox("Revert Manual Changes on Zone Change",
            "Restores the old behaviour of reverting your character to its game or automation base whenever you change the zone.",
            _config.RevertManualChangesOnZoneChange, v => _config.RevertManualChangesOnZoneChange = v);
        ImGui.NewLine();
    }

    private void DrawInterfaceSettings()
    {
        if (!ImGui.CollapsingHeader("Interface"))
            return;

        Checkbox("Show Quick Design Bar",
            "Show a bar separate from the main window that allows you to quickly apply designs or revert your character and target.",
            _config.ShowDesignQuickBar, v => _config.ShowDesignQuickBar = v);
        Checkbox("Lock Quick Design Bar", "Prevent the quick design bar from being moved and lock it in place.", _config.LockDesignQuickBar,
            v => _config.LockDesignQuickBar = v);
        if (Widget.ModifiableKeySelector("Hotkey to Toggle Quick Design Bar", "Set a hotkey that opens or closes the quick design bar.",
                100 * ImGuiHelpers.GlobalScale,
                _config.ToggleQuickDesignBar, v => _config.ToggleQuickDesignBar = v, _validKeys))
            _config.Save();
        Checkbox("Show Quick Design Bar in Main Window",
            "Show the quick design bar in the tab selection part of the main window, too.",
            _config.ShowQuickBarInTabs, v => _config.ShowQuickBarInTabs = v);

        ImGui.Dummy(Vector2.Zero);
        ImGui.Separator();
        ImGui.Dummy(Vector2.Zero);

        Checkbox("Enable Game Context Menus", "Whether to show a Try On via Glamourer button on context menus for equippable items.",
            _config.EnableGameContextMenu,    v =>
            {
                _config.EnableGameContextMenu = v;
                if (v)
                    _contextMenuService.Enable();
                else
                    _contextMenuService.Disable();
            });
        Checkbox("Hide Window in Cutscenes", "Whether the main Glamourer window should automatically be hidden when entering cutscenes or not.",
            _config.HideWindowInCutscene,
            v =>
            {
                _config.HideWindowInCutscene     = v;
                _uiBuilder.DisableCutsceneUiHide = !v;
            });
        Checkbox("Lock Main Window", "Prevent the main window from being moved and lock it in place.", _config.LockMainWindow,
            v => _config.LockMainWindow = v);
        Checkbox("Open Main Window at Game Start", "Whether the main Glamourer window should be open or closed after launching the game.",
            _config.OpenWindowAtStart,             v => _config.OpenWindowAtStart = v);
        ImGui.Dummy(Vector2.Zero);
        ImGui.Separator();
        ImGui.Dummy(Vector2.Zero);

        Checkbox("Smaller Equip Display", "Use single-line display without icons and small dye buttons instead of double-line display.",
            _config.SmallEquip,           v => _config.SmallEquip = v);
        Checkbox("Show Application Checkboxes",
            "Show the application checkboxes in the Customization and Equipment panels of the design tab, instead of only showing them under Application Rules.",
            !_config.HideApplyCheckmarks, v => _config.HideApplyCheckmarks = !v);
        if (Widget.DoubleModifierSelector("Design Deletion Modifier",
                "A modifier you need to hold while clicking the Delete Design button for it to take effect.", 100 * ImGuiHelpers.GlobalScale,
                _config.DeleteDesignModifier, v => _config.DeleteDesignModifier = v))
            _config.Save();
        Checkbox("Auto-Open Design Folders",
            "Have design folders open or closed as their default state after launching.", _config.OpenFoldersByDefault,
            v => _config.OpenFoldersByDefault = v);
        DrawFolderSortType();

        ImGui.Dummy(Vector2.Zero);
        ImGui.Separator();
        ImGui.Dummy(Vector2.Zero);

        Checkbox("Show all Application Rule Checkboxes for Automation",
            "Show multiple separate application rule checkboxes for automated designs, instead of a single box for enabling or disabling.",
            _config.ShowAllAutomatedApplicationRules, v => _config.ShowAllAutomatedApplicationRules = v);
        Checkbox("Show Unobtained Item Warnings",
            "Show information whether you have unlocked all items and customizations in your automated design or not.",
            _config.ShowUnlockedItemWarnings, v => _config.ShowUnlockedItemWarnings = v);
        Checkbox("Debug Mode", "Show the debug tab. Only useful for debugging or advanced use. Not recommended in general.", _config.DebugMode,
            v => _config.DebugMode = v);
        ImGui.NewLine();
    }

    /// <summary> Draw the entire Color subsection. </summary>
    private void DrawColorSettings()
    {
        if (!ImGui.CollapsingHeader("Colors"))
            return;

        foreach (var color in Enum.GetValues<ColorId>())
        {
            var (defaultColor, name, description) = color.Data();
            var currentColor = _config.Colors.TryGetValue(color, out var current) ? current : defaultColor;
            if (Widget.ColorPicker(name, description, currentColor, c => _config.Colors[color] = c, defaultColor))
                _config.Save();
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
            var       color = _codeService.CheckCode(_currentCode) != null ? ColorId.ActorAvailable : ColorId.ActorUnavailable;
            using var c     = ImRaii.PushColor(ImGuiCol.Border, color.Value(), _currentCode.Length > 0);
            if (ImGui.InputTextWithHint("##Code", "Enter Cheat Code...", ref _currentCode, 512, ImGuiInputTextFlags.EnterReturnsTrue))
                if (_codeService.AddCode(_currentCode))
                    _currentCode = string.Empty;
        }

        ImGui.SameLine();
        ImGuiComponents.HelpMarker(tooltip);

        DrawCodeHints();

        if (_config.Codes.Count <= 0)
            return;

        for (var i = 0; i < _config.Codes.Count; ++i)
        {
            var (code, state) = _config.Codes[i];
            var action = _codeService.CheckCode(code);
            if (action == null)
                continue;

            if (ImGui.Checkbox(code, ref state))
            {
                action(state);
                _config.Codes[i] = (code, state);
                _codeService.VerifyState();
                _config.Save();
            }
        }

        if (_codeService.EnabledCaptain)
        {
            if (ImGui.Button("Who am I?!?"))
                _funModule.WhoAmI();

            ImGui.SameLine();

            if (ImGui.Button("Who is that!?!"))
                _funModule.WhoIsThat();
        }
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
            _config.Save();
        }

        ImGui.SameLine();
        ImGuiUtil.LabeledHelpMarker(label, tooltip);
    }

    /// <summary> Different supported sort modes as a combo. </summary>
    private void DrawFolderSortType()
    {
        var sortMode = _config.SortMode;
        ImGui.SetNextItemWidth(300 * ImGuiHelpers.GlobalScale);
        using (var combo = ImRaii.Combo("##sortMode", sortMode.Name))
        {
            if (combo)
                foreach (var val in Configuration.Constants.ValidSortModes)
                {
                    if (ImGui.Selectable(val.Name, val.GetType() == sortMode.GetType()) && val.GetType() != sortMode.GetType())
                    {
                        _config.SortMode = val;
                        _selector.SetFilterDirty();
                        _config.Save();
                    }

                    ImGuiUtil.HoverTooltip(val.Description);
                }
        }

        ImGuiUtil.LabeledHelpMarker("Sort Mode", "Choose the sort mode for the mod selector in the designs tab.");
    }
}
