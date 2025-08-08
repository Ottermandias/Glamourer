using Glamourer.Services;
using Dalamud.Bindings.ImGui;
using OtterGui.Services;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Equipment;

public class ItemCopyService(ItemManager items, DictStain stainData) : IUiService
{
    public EquipItem? Item  { get; private set; }
    public Stain?     Stain { get; private set; }

    public void Copy(in EquipItem item)
        => Item = item;

    public void Copy(in Stain stain)
        => Stain = stain;

    public void Paste(int which, Action<int, StainId> setter)
    {
        if (Stain is { } stain)
            setter(which, stain.RowIndex);
    }

    public void Paste(FullEquipType type, Action<EquipItem> setter)
    {
        if (Item is not { } item)
            return;

        if (type != item.Type)
        {
            if (type.IsBonus())
                item = items.Identify(type.ToBonus(), item.PrimaryId, item.Variant);
            else if (type.IsEquipment() || type.IsAccessory())
                item = items.Identify(type.ToSlot(), item.PrimaryId, item.Variant);
            else
                item = items.Identify(type.ToSlot(), item.PrimaryId, item.SecondaryId, item.Variant);
        }

        if (item.Valid && item.Type == type)
            setter(item);
    }

    public void HandleCopyPaste(in EquipDrawData data)
    {
        if (ImGui.GetIO().KeyCtrl)
        {
            if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Middle))
                Paste(data.CurrentItem.Type, data.SetItem);
        }
        else if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled) && ImGui.IsMouseClicked(ImGuiMouseButton.Middle))
        {
            Copy(data.CurrentItem);
        }
    }

    public void HandleCopyPaste(in EquipDrawData data, int which)
    {
        if (ImGui.GetIO().KeyCtrl)
        {
            if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Middle))
                Paste(which, data.SetStain);
        }
        else if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)
              && ImGui.IsMouseClicked(ImGuiMouseButton.Middle)
              && stainData.TryGetValue(data.CurrentStains[which].Id, out var stain))
        {
            Copy(stain);
        }
    }
}
