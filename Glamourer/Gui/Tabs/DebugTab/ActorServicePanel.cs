using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Utility;
using Glamourer.Services;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using ImGuiClip = OtterGui.ImGuiClip;

namespace Glamourer.Gui.Tabs.DebugTab;

public class ActorServicePanel(ActorService _actors, ItemManager _items) : IDebugTabTree
{
    public string Label
        => "Actor Service";

    public bool Disabled
        => !_actors.Valid;

    private string _bnpcFilter      = string.Empty;
    private string _enpcFilter      = string.Empty;
    private string _companionFilter = string.Empty;
    private string _mountFilter     = string.Empty;
    private string _ornamentFilter  = string.Empty;
    private string _worldFilter     = string.Empty;

    public void Draw()
    {
        DrawBnpcTable();
        DebugTab.DrawNameTable("ENPCs",      ref _enpcFilter,      _actors.AwaitedService.Data.ENpcs.Select(kvp => (kvp.Key, kvp.Value)));
        DebugTab.DrawNameTable("Companions", ref _companionFilter, _actors.AwaitedService.Data.Companions.Select(kvp => (kvp.Key, kvp.Value)));
        DebugTab.DrawNameTable("Mounts",     ref _mountFilter,     _actors.AwaitedService.Data.Mounts.Select(kvp => (kvp.Key, kvp.Value)));
        DebugTab.DrawNameTable("Ornaments",  ref _ornamentFilter,  _actors.AwaitedService.Data.Ornaments.Select(kvp => (kvp.Key, kvp.Value)));
        DebugTab.DrawNameTable("Worlds",     ref _worldFilter,     _actors.AwaitedService.Data.Worlds.Select(kvp => ((uint)kvp.Key, kvp.Value)));
    }

    private void DrawBnpcTable()
    {
        using var _    = ImRaii.PushId(1);
        using var tree = ImRaii.TreeNode("BNPCs");
        if (!tree)
            return;

        var resetScroll = ImGui.InputTextWithHint("##filter", "Filter...", ref _bnpcFilter, 256);
        var height      = ImGui.GetTextLineHeightWithSpacing() + 2 * ImGui.GetStyle().CellPadding.Y;
        using var table = ImRaii.Table("##table", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.BordersOuter,
            new Vector2(-1, 10 * height));
        if (!table)
            return;

        if (resetScroll)
            ImGui.SetScrollY(0);
        ImGui.TableSetupColumn("1", ImGuiTableColumnFlags.WidthFixed, 50 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("2", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("3", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextColumn();
        var skips = ImGuiClip.GetNecessarySkips(height);
        ImGui.TableNextRow();
        var data = _actors.AwaitedService.Data.BNpcs.Select(kvp => (kvp.Key, kvp.Key.ToString("D5"), kvp.Value));
        var remainder = ImGuiClip.FilteredClippedDraw(data, skips,
            p => p.Item2.Contains(_bnpcFilter) || p.Item3.Contains(_bnpcFilter, StringComparison.OrdinalIgnoreCase),
            p =>
            {
                ImGuiUtil.DrawTableColumn(p.Item2);
                ImGuiUtil.DrawTableColumn(p.Item3);
                var bnpcs = _items.IdentifierService.AwaitedService.GetBnpcsFromName(p.Item1);
                ImGuiUtil.DrawTableColumn(string.Join(", ", bnpcs.Select(b => b.Id.ToString())));
            });
        ImGuiClip.DrawEndDummy(remainder, height);
    }
}
