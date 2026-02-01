using FFXIVClientStructs.FFXIV.Client.Game;
using Dalamud.Bindings.ImGui;
using ImSharp;
using OtterGui;
using OtterGui.Raii;
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
        if (inventory == null)
            return;

        ImGuiUtil.CopyOnClickSelectable($"0x{(ulong)inventory:X}");

        var equip = inventory->GetInventoryContainer(InventoryType.EquippedItems);
        if (equip == null || equip->IsLoaded)
            return;

        ImGuiUtil.CopyOnClickSelectable($"0x{(ulong)equip:X}");

        using var table = Im.Table.Begin("items"u8, 4, TableFlags.RowBackground | TableFlags.SizingFixedFit);
        if (!table)
            return;

        for (var i = 0; i < equip->Size; ++i)
        {
            ImGuiUtil.DrawTableColumn(i.ToString());
            var item = equip->GetInventorySlot(i);
            if (item == null)
            {
                ImGuiUtil.DrawTableColumn("NULL");
                ImGui.TableNextRow();
            }
            else
            {
                ImGuiUtil.DrawTableColumn(item->ItemId.ToString());
                ImGuiUtil.DrawTableColumn(item->GlamourId.ToString());
                ImGui.TableNextColumn();
                ImGuiUtil.CopyOnClickSelectable($"0x{(ulong)item:X}");
            }
        }
    }
}
