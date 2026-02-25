using FFXIVClientStructs.FFXIV.Client.Game;
using ImSharp;
using Penumbra.GameData.Gui.Debug;

namespace Glamourer.Gui.Tabs.DebugTab;

public sealed unsafe class InventoryPanel : IGameDataDrawer
{
    public ReadOnlySpan<byte> Label
        => "Inventory"u8;

    public bool Disabled
        => false;

    public void Draw()
    {
        var inventory = InventoryManager.Instance();
        if (inventory is null)
            return;

        Glamourer.Dynamis.DrawPointer(inventory);

        var equip = inventory->GetInventoryContainer(InventoryType.EquippedItems);
        if (equip is null || equip->IsLoaded)
            return;

        Glamourer.Dynamis.DrawPointer(equip);

        using var table = Im.Table.Begin("items"u8, 4, TableFlags.RowBackground | TableFlags.SizingFixedFit);
        if (!table)
            return;

        for (var i = 0; i < equip->Size; ++i)
        {
            table.DrawColumn($"{i}");
            var item = equip->GetInventorySlot(i);
            if (item is null)
            {
                table.DrawColumn("NULL"u8);
                table.NextRow();
            }
            else
            {
                table.DrawColumn($"{item->ItemId}");
                table.DrawColumn($"{item->GlamourId}");
                table.NextColumn();
                Glamourer.Dynamis.DrawPointer(item);
            }
        }
    }
}
