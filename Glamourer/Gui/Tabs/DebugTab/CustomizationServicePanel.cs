using System;
using Glamourer.Customization;
using Glamourer.Services;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;

namespace Glamourer.Gui.Tabs.DebugTab;

public class CustomizationServicePanel(CustomizationService _customization) : IDebugTabTree
{
    public string Label
        => "Customization Service";

    public bool Disabled
        => !_customization.Awaiter.IsCompletedSuccessfully;

    public void Draw()
    {
        foreach (var clan in _customization.Service.Clans)
        {
            foreach (var gender in _customization.Service.Genders)
            {
                var set = _customization.Service.GetList(clan, gender);
                DrawCustomizationInfo(set);
                DrawNpcCustomizationInfo(set);
            }
        }
    }

    private void DrawCustomizationInfo(CustomizationSet set)
    {
        using var tree = ImRaii.TreeNode($"{_customization.ClanName(set.Clan, set.Gender)} {set.Gender}");
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

    private void DrawNpcCustomizationInfo(CustomizationSet set)
    {
        using var tree = ImRaii.TreeNode($"{_customization.ClanName(set.Clan, set.Gender)} {set.Gender} (NPC Options)");
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
