using System;
using System.Runtime.CompilerServices;
using Dalamud.Interface;
using Glamourer.Gui.Tabs.DesignTab;
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
    private readonly Configuration            _config;
    private readonly DesignFileSystemSelector _selector;
    private readonly StateListener            _stateListener;
    private readonly PhrasingService          _phrasingService;
    private readonly PenumbraAutoRedraw       _autoRedraw;

    public SettingsTab(Configuration config, DesignFileSystemSelector selector, StateListener stateListener,
        PhrasingService phrasingService, PenumbraAutoRedraw autoRedraw)
    {
        _config          = config;
        _selector        = selector;
        _stateListener   = stateListener;
        _phrasingService = phrasingService;
        _autoRedraw      = autoRedraw;
    }

    public ReadOnlySpan<byte> Label
        => "Settings"u8;

    private string? _tmpPhrasing1 = null;
    private string? _tmpPhrasing2 = null;

    public void DrawContent()
    {
        using var child = ImRaii.Child("MainWindowChild");
        if (!child)
            return;

        Checkbox("Enabled", "Enable main functionality of keeping and applying state.", _stateListener.Enabled, _stateListener.Enable);
        Checkbox("Enable Auto Designs", "Enable the application of designs associated to characters to be applied automatically.",
            _config.EnableAutoDesigns,  v => _config.EnableAutoDesigns = v);
        Checkbox("Restricted Gear Protection",
            "Use gender- and race-appropriate models when detecting certain items not available for a characters current gender and race.",
            _config.UseRestrictedGearProtection, v => _config.UseRestrictedGearProtection = v);
        Checkbox("Auto-Reload Gear",
            "Automatically reload equipment pieces on your own character when changing any mod options in Penumbra in their associated collection.",
            _config.AutoRedrawEquipOnChanges, _autoRedraw.SetState);
        if (Widget.DoubleModifierSelector("Design Deletion Modifier",
                "A modifier you need to hold while clicking the Delete Design button for it to take effect.", 100 * ImGuiHelpers.GlobalScale,
                _config.DeleteDesignModifier, v => _config.DeleteDesignModifier = v))
            _config.Save();
        DrawFolderSortType();
        Checkbox("Auto-Open Design Folders",
            "Have design folders open or closed as their default state after launching.", _config.OpenFoldersByDefault,
            v => _config.OpenFoldersByDefault = v);
        Checkbox("Debug Mode", "Show the debug tab. Only useful for debugging or advanced use.", _config.DebugMode, v => _config.DebugMode = v);
        DrawColorSettings();

        _tmpPhrasing1 ??= _config.Phrasing1;
        ImGui.InputText("Phrasing 1", ref _tmpPhrasing1, 512);

        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            _phrasingService.SetPhrasing1(_tmpPhrasing1);
            _tmpPhrasing1 = null;
        }

        _tmpPhrasing2 ??= _config.Phrasing2;
        ImGui.InputText("Phrasing 2", ref _tmpPhrasing2, 512);

        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            _phrasingService.SetPhrasing2(_tmpPhrasing2);
            _tmpPhrasing2 = null;
        }

        MainWindow.DrawSupportButtons();
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
