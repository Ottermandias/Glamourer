using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dalamud.Interface;
using Glamourer.Designs;
using Glamourer.Util;
using ImGuiNET;
using OtterGui;
using OtterGui.Widgets;
using Penumbra.GameData.Data;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Equipment;

public partial class EquipmentDrawer
{
    private readonly FilterComboColors _stainCombo;
    private readonly StainData         _stainData;
    private readonly ItemCombo[]       _itemCombo;

    public EquipmentDrawer(ItemManager items)
    {
        _stainData = items.Stains;
        _stainCombo = new FilterComboColors(140,
            _stainData.Data.Prepend(new KeyValuePair<byte, (string Name, uint Dye, bool Gloss)>(0, ("None", 0, false))));
        _itemCombo = EquipSlotExtensions.EqdpSlots.Select(e => new ItemCombo(items, e)).ToArray();
    }

    public bool DrawArmor(DesignBase design, EquipSlot slot, out Item armor)
    {
        Debug.Assert(slot.IsEquipment() || slot.IsAccessory());
        var combo = _itemCombo[slot.ToIndex()];
        armor = design.Armor(slot);
        var change = combo.Draw(armor.Name, armor.ItemId, 320 * ImGuiHelpers.GlobalScale);
        if (armor.ModelBase.Value != 0)
        {
            ImGuiUtil.HoverTooltip("Right-click to clear.");
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                change = true;
                armor  = ItemManager.NothingItem(slot);
            }
        }
        else if (change)
        {
            armor = combo.CurrentSelection.WithStain(armor.Stain);
        }

        return change;
    }

    public bool DrawStain(DesignBase design, EquipSlot slot, out Stain stain)
    {
        Debug.Assert(slot.IsEquipment() || slot.IsAccessory());
        var armor = design.Armor(slot);
        var found = _stainData.TryGetValue(armor.Stain, out stain);
        if (!_stainCombo.Draw($"##stain{slot}", stain.RgbaColor, found))
            return false;

        return _stainData.TryGetValue(_stainCombo.CurrentSelection.Key, out stain);
    }
}
