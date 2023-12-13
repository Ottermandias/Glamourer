using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;

namespace Glamourer.Gui.Tabs.DebugTab;

public unsafe class InventoryPanel : IDebugTabTree
{
    public string Label
        => "Inventory";

    public bool Disabled
        => false;

    public void Draw()
    {
        var inventory = InventoryManager.Instance();
        if (inventory == null)
            return;

        ImGuiUtil.CopyOnClickSelectable($"0x{(ulong)inventory:X}");

        var equip = inventory->GetInventoryContainer(InventoryType.EquippedItems);
        if (equip == null || equip->Loaded == 0)
            return;

        ImGuiUtil.CopyOnClickSelectable($"0x{(ulong)equip:X}");

        using var table = ImRaii.Table("items", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit);
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
                ImGuiUtil.DrawTableColumn(item->ItemID.ToString());
                ImGuiUtil.DrawTableColumn(item->GlamourID.ToString());
                ImGui.TableNextColumn();
                ImGuiUtil.CopyOnClickSelectable($"0x{(ulong)item:X}");
            }
        }
    }
}
