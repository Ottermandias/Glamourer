using Glamourer.Automation;
using Dalamud.Bindings.ImGui;
using OtterGui;
using OtterGui.Extensions;
using OtterGui.Raii;
using Penumbra.GameData.Gui.Debug;

namespace Glamourer.Gui.Tabs.DebugTab;

public sealed class AutoDesignPanel(AutoDesignManager autoDesignManager) : IGameDataDrawer
{
    public ReadOnlySpan<byte> Label
        => "Auto Designs"u8;

    public bool Disabled
        => false;

    public void Draw()
    {
        foreach (var (set, idx) in autoDesignManager.WithIndex())
        {
            using var id   = ImRaii.PushId(idx);
            using var tree = ImRaii.TreeNode(set.Name);
            if (!tree)
                continue;

            using var table = ImRaii.Table("##autoDesign", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg);
            if (!table)
                continue;

            ImGuiUtil.DrawTableColumn("Name");
            ImGuiUtil.DrawTableColumn(set.Name);

            ImGuiUtil.DrawTableColumn("Index");
            ImGuiUtil.DrawTableColumn(idx.ToString());

            ImGuiUtil.DrawTableColumn("Enabled");
            ImGuiUtil.DrawTableColumn(set.Enabled.ToString());

            ImGuiUtil.DrawTableColumn("Actor");
            ImGuiUtil.DrawTableColumn(set.Identifiers[0].ToString());

            foreach (var (design, designIdx) in set.Designs.WithIndex())
            {
                ImGuiUtil.DrawTableColumn($"{design.Design.ResolveName(false)} ({designIdx})");
                ImGuiUtil.DrawTableColumn($"{design.Type} {design.Jobs.Name}");
            }
        }
    }
}
