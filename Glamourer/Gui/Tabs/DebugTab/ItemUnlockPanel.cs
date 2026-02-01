using Dalamud.Interface.Utility;
using Glamourer.Services;
using Glamourer.Unlocks;
using Dalamud.Bindings.ImGui;
using ImSharp;
using OtterGui;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Gui.Debug;
using ImGuiClip = OtterGui.ImGuiClip;

namespace Glamourer.Gui.Tabs.DebugTab;

public sealed class ItemUnlockPanel(ItemUnlockManager itemUnlocks, ItemManager items) : IGameDataDrawer
{
    public ReadOnlySpan<byte> Label
        => "Unlocked Items"u8;

    public bool Disabled
        => false;

    public void Draw()
    {
        using var table = Im.Table.Begin("itemUnlocks"u8, 5,
            TableFlags.SizingFixedFit | TableFlags.RowBackground | TableFlags.ScrollY | TableFlags.BordersOuter,
            Im.ContentRegion.Available with { Y = 12 * Im.Style.TextHeight });
        if (!table)
            return;

        table.SetupColumn("ItemId"u8, TableColumnFlags.WidthFixed, 30 * Im.Style.GlobalScale);
        table.SetupColumn("Name"u8,   TableColumnFlags.WidthFixed, 400 * Im.Style.GlobalScale);
        table.SetupColumn("Slot"u8,   TableColumnFlags.WidthFixed, 120 * Im.Style.GlobalScale);
        table.SetupColumn("Model"u8,  TableColumnFlags.WidthFixed, 80 * Im.Style.GlobalScale);
        table.SetupColumn("Unlock"u8, TableColumnFlags.WidthFixed, 120 * Im.Style.GlobalScale);

        ImGui.TableNextColumn();
        var skips = ImGuiClip.GetNecessarySkips(Im.Style.TextHeightWithSpacing);
        ImGui.TableNextRow();
        var remainder = ImGuiClip.ClippedDraw(itemUnlocks, skips, t =>
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
        }, itemUnlocks.Count);
        ImGuiClip.DrawEndDummy(remainder, Im.Style.TextHeight);
    }
}
