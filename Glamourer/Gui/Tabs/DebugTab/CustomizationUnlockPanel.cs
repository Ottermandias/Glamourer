using Glamourer.Unlocks;
using Dalamud.Bindings.ImGui;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Gui.Debug;

namespace Glamourer.Gui.Tabs.DebugTab;

public sealed class CustomizationUnlockPanel(CustomizeUnlockManager customizeUnlocks) : IGameDataDrawer
{
    public ReadOnlySpan<byte> Label
        => "Customizations"u8;

    public bool Disabled
        => false;

    public void Draw()
    {
        using var table = ImRaii.Table("customizationUnlocks", 6,
            ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.BordersOuter,
            new Vector2(ImGui.GetContentRegionAvail().X, 12 * ImGui.GetTextLineHeight()));
        if (!table)
            return;

        ImGui.TableNextColumn();
        var skips = ImGuiClip.GetNecessarySkips(ImGui.GetTextLineHeightWithSpacing());
        ImGui.TableNextRow();
        var remainder = ImGuiClip.ClippedDraw(customizeUnlocks.Unlockable, skips, t =>
        {
            ImGuiUtil.DrawTableColumn(t.Key.Index.ToDefaultName());
            ImGuiUtil.DrawTableColumn(t.Key.CustomizeId.ToString());
            ImGuiUtil.DrawTableColumn(t.Key.Value.Value.ToString());
            ImGuiUtil.DrawTableColumn(t.Value.Data.ToString());
            ImGuiUtil.DrawTableColumn(t.Value.Name);
            ImGuiUtil.DrawTableColumn(customizeUnlocks.IsUnlocked(t.Key, out var time)
                ? time == DateTimeOffset.MinValue
                    ? "Always"
                    : time.LocalDateTime.ToString("g")
                : "Never");
        }, customizeUnlocks.Unlockable.Count);
        ImGuiClip.DrawEndDummy(remainder, ImGui.GetTextLineHeight());
    }
}
