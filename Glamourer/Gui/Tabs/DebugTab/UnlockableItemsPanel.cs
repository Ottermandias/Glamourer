using Dalamud.Interface.Utility;
using Glamourer.Services;
using Glamourer.Unlocks;
using Dalamud.Bindings.ImGui;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Gui.Debug;
using ImGuiClip = OtterGui.ImGuiClip;

namespace Glamourer.Gui.Tabs.DebugTab;

public sealed class UnlockableItemsPanel(ItemUnlockManager itemUnlocks, ItemManager items) : IGameDataDrawer
{
    public ReadOnlySpan<byte> Label
        => "Unlockable Items"u8;

    public bool Disabled
        => false;

    public void Draw()
    {
        using var table = ImRaii.Table("unlockableItem", 6,
            ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.BordersOuter,
            new Vector2(ImGui.GetContentRegionAvail().X, 12 * ImGui.GetTextLineHeight()));
        if (!table)
            return;

        ImGui.TableSetupColumn("ItemId",   ImGuiTableColumnFlags.WidthFixed, 30 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Name",     ImGuiTableColumnFlags.WidthFixed, 400 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Slot",     ImGuiTableColumnFlags.WidthFixed, 120 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Model",    ImGuiTableColumnFlags.WidthFixed, 80 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Unlock",   ImGuiTableColumnFlags.WidthFixed, 120 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Criteria", ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableNextColumn();
        var skips = ImGuiClip.GetNecessarySkips(ImGui.GetTextLineHeightWithSpacing());
        ImGui.TableNextRow();
        var remainder = ImGuiClip.ClippedDraw(itemUnlocks.Unlockable, skips, t =>
        {
            ImGuiUtil.DrawTableColumn(t.Key.ToString());
            if (items.ItemData.TryGetValue(t.Key, EquipSlot.MainHand, out var equip))
            {
                ImGuiUtil.DrawTableColumn(equip.Name);
                ImGuiUtil.DrawTableColumn(equip.Type.ToName());
                ImGuiUtil.DrawTableColumn(equip.Weapon().ToString());
            }
            else
            {
                ImGui.TableNextColumn();
                ImGui.TableNextColumn();
                ImGui.TableNextColumn();
            }

            ImGuiUtil.DrawTableColumn(itemUnlocks.IsUnlocked(t.Key, out var time)
                ? time == DateTimeOffset.MinValue
                    ? "Always"
                    : time.LocalDateTime.ToString("g")
                : "Never");
            ImGuiUtil.DrawTableColumn(t.Value.ToString());
        }, itemUnlocks.Unlockable.Count);
        ImGuiClip.DrawEndDummy(remainder, ImGui.GetTextLineHeight());
    }
}
