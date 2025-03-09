using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData.Gui.Debug;

namespace Glamourer.Gui.Tabs.DebugTab;

public unsafe class InventoryPanel : IGameDataDrawer
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
        if (equip == null || equip->IsLoaded)
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
                ImGuiUtil.DrawTableColumn(item->ItemId.ToString());
                ImGuiUtil.DrawTableColumn(item->GlamourId.ToString());
                ImGui.TableNextColumn();
                ImGuiUtil.CopyOnClickSelectable($"0x{(ulong)item:X}");
            }
        }
    }
}
