using System;
using Glamourer.GameData;
using Glamourer.Services;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Gui.Debug;

namespace Glamourer.Gui.Tabs.DebugTab;

public class CustomizationServicePanel(CustomizeService customize) : IGameDataDrawer
{
    public string Label
        => "Customization Service";

    public bool Disabled
        => !customize.Finished;

    public void Draw()
    {
        foreach (var (clan, gender) in CustomizeManager.AllSets())
        {
            var set = customize.Manager.GetSet(clan, gender);
            DrawCustomizationInfo(set);
            DrawNpcCustomizationInfo(set);
        }
    }

    private void DrawCustomizationInfo(CustomizeSet set)
    {
        using var tree = ImRaii.TreeNode($"{customize.ClanName(set.Clan, set.Gender)} {set.Gender}");
        if (!tree)
            return;

        using var table = ImRaii.Table("data", 5, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg);
        if (!table)
            return;

        foreach (var index in Enum.GetValues<CustomizeIndex>())
        {
            ImGuiUtil.DrawTableColumn(index.ToString());
            ImGuiUtil.DrawTableColumn(set.Option(index));
            ImGuiUtil.DrawTableColumn(set.IsAvailable(index) ? "Available" : "Unavailable");
            ImGuiUtil.DrawTableColumn(set.Type(index).ToString());
            ImGuiUtil.DrawTableColumn(set.Count(index).ToString());
        }
    }

    private void DrawNpcCustomizationInfo(CustomizeSet set)
    {
        using var tree = ImRaii.TreeNode($"{customize.ClanName(set.Clan, set.Gender)} {set.Gender} (NPC Options)");
        if (!tree)
            return;

        using var table = ImRaii.Table("npc", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg);
        if (!table)
            return;

        foreach (var (index, value) in set.NpcOptions)
        {
            ImGuiUtil.DrawTableColumn(index.ToString());
            ImGuiUtil.DrawTableColumn(value.Value.ToString());
        }
    }
}
