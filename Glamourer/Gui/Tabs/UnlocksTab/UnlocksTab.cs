using System;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;
using OtterGui.Raii;
using OtterGui;
using OtterGui.Widgets;

namespace Glamourer.Gui.Tabs.UnlocksTab;

public class UnlocksTab : ITab
{
    private readonly Configuration  _config;
    private readonly UnlockOverview _overview;
    private readonly UnlockTable    _table;

    public UnlocksTab(Configuration config, UnlockOverview overview, UnlockTable table)
    {
        _config   = config;
        _overview = overview;
        _table    = table;
    }

    private bool DetailMode
    {
        get => _config.UnlockDetailMode;
        set
        {
            _config.UnlockDetailMode = value;
            _config.Save();
        }
    }

    public ReadOnlySpan<byte> Label
        => "Unlocks"u8;

    public void DrawContent()
    {
        DrawTypeSelection();
        if (DetailMode)
            _table.Draw(ImGui.GetFrameHeightWithSpacing());
        else
            _overview.Draw();
    }

    private void DrawTypeSelection()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
            .Push(ImGuiStyleVar.FrameRounding, 0);
        var buttonSize = new Vector2(ImGui.GetContentRegionAvail().X / 2, ImGui.GetFrameHeight());

        if (ImGuiUtil.DrawDisabledButton("Overview Mode", buttonSize, "Show tinted icons of sets of unlocks.", !DetailMode))
            DetailMode = false;

        ImGui.SameLine();
        if (ImGuiUtil.DrawDisabledButton("Detailed Mode", buttonSize, "Show all unlockable data as a combined filterable and sortable table.",
                DetailMode))
            DetailMode = true;
    }
}
