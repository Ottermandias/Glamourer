using Dalamud.Interface;
using Glamourer.GameData;
using Glamourer.Services;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Text;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Gui.Debug;
using Penumbra.GameData.Structs;

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

        DrawColorInfo();
    }

    private void DrawColorInfo()
    {
        using var tree = ImUtf8.TreeNode("NPC Colors"u8);
        if (!tree)
            return;

        using var table = ImUtf8.Table("data"u8, 5, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg);
        if (!table)
            return;

        ImGui.TableNextColumn();
        ImUtf8.TableHeader("Id"u8);
        ImGui.TableNextColumn();
        ImUtf8.TableHeader("Hair"u8);
        ImGui.TableNextColumn();
        ImUtf8.TableHeader("Eyes"u8);
        ImGui.TableNextColumn();
        ImUtf8.TableHeader("Facepaint"u8);
        ImGui.TableNextColumn();
        ImUtf8.TableHeader("Tattoos"u8);

        for (var i = 192; i < 256; ++i)
        {
            var index = new CustomizeValue((byte)i);
            ImUtf8.DrawTableColumn($"{i:D3}");
            using var font = ImRaii.PushFont(UiBuilder.IconFont);
            ImUtf8.DrawTableColumn(customize.NpcCustomizeSet.CheckColor(CustomizeIndex.HairColor, index)
                ? FontAwesomeIcon.Check.ToIconString()
                : FontAwesomeIcon.Times.ToIconString());
            ImUtf8.DrawTableColumn(customize.NpcCustomizeSet.CheckColor(CustomizeIndex.EyeColorLeft, index)
                ? FontAwesomeIcon.Check.ToIconString()
                : FontAwesomeIcon.Times.ToIconString());
            ImUtf8.DrawTableColumn(customize.NpcCustomizeSet.CheckColor(CustomizeIndex.FacePaintColor, index)
                ? FontAwesomeIcon.Check.ToIconString()
                : FontAwesomeIcon.Times.ToIconString());
            ImUtf8.DrawTableColumn(customize.NpcCustomizeSet.CheckColor(CustomizeIndex.TattooColor, index)
                ? FontAwesomeIcon.Check.ToIconString()
                : FontAwesomeIcon.Times.ToIconString());
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
