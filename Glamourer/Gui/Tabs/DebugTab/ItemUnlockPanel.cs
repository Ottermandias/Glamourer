using Glamourer.Services;
using Glamourer.Unlocks;
using ImSharp;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Gui.Debug;

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

        using var clipper = new Im.ListClipper(itemUnlocks.Count, Im.Style.TextHeightWithSpacing);
        foreach (var (id, _) in clipper.Iterate(itemUnlocks))
        {
            table.DrawColumn($"{id}");
            if (items.ItemData.TryGetValue(id, EquipSlot.MainHand, out var equip))
            {
                table.DrawColumn(equip.Name);
                table.DrawColumn(equip.Type.ToName());
                table.DrawColumn($"{equip.Weapon()}");
            }
            else
            {
                table.NextColumn();
                table.NextColumn();
                table.NextColumn();
            }

            table.DrawColumn(itemUnlocks.IsUnlocked(id, out var time)
                ? time == DateTimeOffset.MinValue
                    ? "Always"u8
                    : $"{time.LocalDateTime:g}"
                : "Never"u8);
        }
    }
}
