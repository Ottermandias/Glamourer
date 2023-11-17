using System;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using OtterGui.Raii;
using OtterGui;
using OtterGui.Widgets;

namespace Glamourer.Gui.Tabs.UnlocksTab;

public class UnlocksTab : Window, ITab
{
    private readonly EphemeralConfig  _config;
    private readonly UnlockOverview _overview;
    private readonly UnlockTable    _table;

    public UnlocksTab(EphemeralConfig config, UnlockOverview overview, UnlockTable table)
        : base("Unlocked Equipment")
    {
        _config   = config;
        _overview = overview;
        _table    = table;

        IsOpen = false;
        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(700, 675),
            MaximumSize = ImGui.GetIO().DisplaySize,
        };
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
        _table.Flags |= ImGuiTableFlags.Resizable;
    }

    public override void Draw()
    {
        DrawContent();
    }

    private void DrawTypeSelection()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
            .Push(ImGuiStyleVar.FrameRounding, 0);
        var buttonSize = new Vector2(ImGui.GetContentRegionAvail().X / 2, ImGui.GetFrameHeight());
        if (!IsOpen)
            buttonSize.X -= ImGui.GetFrameHeight() / 2;
        if (DetailMode)
            buttonSize.X -= ImGui.GetFrameHeight() / 2;

        if (ImGuiUtil.DrawDisabledButton("Overview Mode", buttonSize, "Show tinted icons of sets of unlocks.", !DetailMode))
            DetailMode = false;

        ImGui.SameLine();
        if (ImGuiUtil.DrawDisabledButton("Detailed Mode", buttonSize, "Show all unlockable data as a combined filterable and sortable table.",
                DetailMode))
            DetailMode = true;

        if (DetailMode)
        {
            ImGui.SameLine();
            if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Expand.ToIconString(), new Vector2(ImGui.GetFrameHeight()),
                    "Restore all columns to their original size.", false, true))
                _table.Flags &= ~ImGuiTableFlags.Resizable;
        }

        if (!IsOpen)
        {
            ImGui.SameLine();
            if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.SquareArrowUpRight.ToIconString(), new Vector2(ImGui.GetFrameHeight()),
                    "Pop the unlocks tab out into its own window.", false, true))
                IsOpen = true;
        }
    }
}
