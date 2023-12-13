using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Utility;
using ImGuiNET;
using Microsoft.Extensions.DependencyInjection;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Widgets;
using ImGuiClip = Dalamud.Interface.Utility.ImGuiClip;

namespace Glamourer.Gui.Tabs.DebugTab;

public unsafe class DebugTab(IServiceProvider _provider) : ITab
{
    private readonly Configuration _config = _provider.GetRequiredService<Configuration>();

    public bool IsVisible
        => _config.DebugMode;

    public ReadOnlySpan<byte> Label
        => "Debug"u8;

    private readonly DebugTabHeader[] _headers =
    [
        DebugTabHeader.CreateInterop(_provider),
        DebugTabHeader.CreateGameData(_provider),
        DebugTabHeader.CreateDesigns(_provider),
        DebugTabHeader.CreateState(_provider),
        DebugTabHeader.CreateUnlocks(_provider),
    ];

    public void DrawContent()
    {
        using var child = ImRaii.Child("MainWindowChild");
        if (!child)
            return;

        foreach (var header in _headers)
            header.Draw();
    }

    public static void DrawInputModelSet(bool withWeapon, ref int setId, ref int secondaryId, ref int variant)
    {
        ImGui.SetNextItemWidth(100 * ImGuiHelpers.GlobalScale);
        ImGui.InputInt("##SetId", ref setId, 0, 0);
        if (withWeapon)
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100 * ImGuiHelpers.GlobalScale);
            ImGui.InputInt("##TypeId", ref secondaryId, 0, 0);
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(100 * ImGuiHelpers.GlobalScale);
        ImGui.InputInt("##Variant", ref variant, 0, 0);
    }

    public static void DrawNameTable(string label, ref string filter, IEnumerable<(uint, string)> names)
    {
        using var _    = ImRaii.PushId(label);
        using var tree = ImRaii.TreeNode(label);
        if (!tree)
            return;

        var resetScroll = ImGui.InputTextWithHint("##filter", "Filter...", ref filter, 256);
        var height      = ImGui.GetTextLineHeightWithSpacing() + 2 * ImGui.GetStyle().CellPadding.Y;
        using var table = ImRaii.Table("##table", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.BordersOuter,
            new Vector2(-1, 10 * height));
        if (!table)
            return;

        if (resetScroll)
            ImGui.SetScrollY(0);
        ImGui.TableSetupColumn("1", ImGuiTableColumnFlags.WidthFixed, 50 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("2", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextColumn();
        var skips = ImGuiClip.GetNecessarySkips(height);
        ImGui.TableNextColumn();
        var f = filter;
        var remainder = ImGuiClip.FilteredClippedDraw(names.Select(p => (p.Item1.ToString("D5"), p.Item2)), skips,
            p => p.Item1.Contains(f) || p.Item2.Contains(f, StringComparison.OrdinalIgnoreCase),
            p =>
            {
                ImGuiUtil.DrawTableColumn(p.Item1);
                ImGuiUtil.DrawTableColumn(p.Item2);
            });
        ImGuiClip.DrawEndDummy(remainder, height);
    }
}
