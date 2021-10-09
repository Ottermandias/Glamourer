using Dalamud.Interface;
using ImGuiNET;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui
{
    internal partial class Interface
    {
        private bool DrawStainSelector(ComboWithFilter<Stain> stainCombo, EquipSlot slot, StainId stainIdx)
        {
            stainCombo.PostPreview = null;
            if (_stains.TryGetValue((byte) stainIdx, out var stain))
            {
                var previewPush = PushColor(stain, ImGuiCol.FrameBg);
                stainCombo.PostPreview = () => ImGui.PopStyleColor(previewPush);
            }

            var change = stainCombo.Draw(string.Empty, out var newStain) && !newStain.RowIndex.Equals(stainIdx);
            if (!change && (byte) stainIdx != 0)
            {
                ImGuiCustom.HoverTooltip("Right-click to clear.");
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    change   = true;
                    newStain = Stain.None;
                }
            }

            if (!change)
                return false;

            if (_player == null)
                return _inDesignMode && (_selection?.Data.WriteStain(slot, newStain.RowIndex) ?? false);

            Glamourer.RevertableDesigns.Add(_player);
            newStain.Write(_player.Address, slot);
            return true;

        }

        private bool DrawItemSelector(ComboWithFilter<Item> equipCombo, Lumina.Excel.GeneratedSheets.Item? item, EquipSlot slot = EquipSlot.Unknown)
        {
            var currentName = item?.Name.ToString() ?? Item.Nothing(slot).Name;
            var change      = equipCombo.Draw(currentName, out var newItem, _itemComboWidth) && newItem.Base.RowId != item?.RowId;
            if (!change && item != null)
            {
                ImGuiCustom.HoverTooltip("Right-click to clear.");
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    change  = true;
                    newItem = Item.Nothing(slot);
                }
            }

            if (!change)
                return false;

            if (_player == null)
                return _inDesignMode && (_selection?.Data.WriteItem(newItem) ?? false);

            Glamourer.RevertableDesigns.Add(_player);
            newItem.Write(_player.Address);
            return true;

        }

        private static bool DrawCheckbox(CharacterEquipMask flag, ref CharacterEquipMask mask)
        {
            var tmp = (uint) mask;
            var ret = false;
            if (ImGui.CheckboxFlags($"##flag_{(uint) flag}", ref tmp, (uint) flag) && tmp != (uint) mask)
            {
                mask = (CharacterEquipMask) tmp;
                ret  = true;
            }

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Enable writing this slot in this save.");
            return ret;
        }

        private bool DrawEquipSlot(EquipSlot slot, CharacterArmor equip)
        {
            var (equipCombo, stainCombo) = _combos[slot];

            var ret = DrawStainSelector(stainCombo, slot, equip.Stain);
            ImGui.SameLine();
            var item = _identifier.Identify(equip.Set, new WeaponType(), equip.Variant, slot);
            ret |= DrawItemSelector(equipCombo, item, slot);

            return ret;
        }

        private bool DrawEquipSlotWithCheck(EquipSlot slot, CharacterArmor equip, CharacterEquipMask flag, ref CharacterEquipMask mask)
        {
            var ret = DrawCheckbox(flag, ref mask);
            ImGui.SameLine();
            ret |= DrawEquipSlot(slot, equip);
            return ret;
        }

        private bool DrawWeapon(EquipSlot slot, CharacterWeapon weapon)
        {
            var (equipCombo, stainCombo) = _combos[slot];

            var ret = DrawStainSelector(stainCombo, slot, weapon.Stain);
            ImGui.SameLine();
            var item = _identifier.Identify(weapon.Set, weapon.Type, weapon.Variant, slot);
            ret |= DrawItemSelector(equipCombo, item, slot);

            return ret;
        }

        private bool DrawWeaponWithCheck(EquipSlot slot, CharacterWeapon weapon, CharacterEquipMask flag, ref CharacterEquipMask mask)
        {
            var ret = DrawCheckbox(flag, ref mask);
            ImGui.SameLine();
            ret |= DrawWeapon(slot, weapon);
            return ret;
        }

        private bool DrawEquip(CharacterEquipment equip)
        {
            var ret = false;
            if (ImGui.CollapsingHeader("Character Equipment"))
            {
                ret |= DrawWeapon(EquipSlot.MainHand, equip.MainHand);
                ret |= DrawWeapon(EquipSlot.OffHand,  equip.OffHand);
                ret |= DrawEquipSlot(EquipSlot.Head,    equip.Head);
                ret |= DrawEquipSlot(EquipSlot.Body,    equip.Body);
                ret |= DrawEquipSlot(EquipSlot.Hands,   equip.Hands);
                ret |= DrawEquipSlot(EquipSlot.Legs,    equip.Legs);
                ret |= DrawEquipSlot(EquipSlot.Feet,    equip.Feet);
                ret |= DrawEquipSlot(EquipSlot.Ears,    equip.Ears);
                ret |= DrawEquipSlot(EquipSlot.Neck,    equip.Neck);
                ret |= DrawEquipSlot(EquipSlot.Wrists,  equip.Wrists);
                ret |= DrawEquipSlot(EquipSlot.RFinger, equip.RFinger);
                ret |= DrawEquipSlot(EquipSlot.LFinger, equip.LFinger);
            }

            return ret;
        }

        private bool DrawEquip(CharacterEquipment equip, ref CharacterEquipMask mask)
        {
            var ret = false;
            if (ImGui.CollapsingHeader("Character Equipment"))
            {
                ret |= DrawWeaponWithCheck(EquipSlot.MainHand, equip.MainHand, CharacterEquipMask.MainHand, ref mask);
                ret |= DrawWeaponWithCheck(EquipSlot.OffHand,  equip.OffHand,  CharacterEquipMask.OffHand,  ref mask);
                ret |= DrawEquipSlotWithCheck(EquipSlot.Head,    equip.Head,    CharacterEquipMask.Head,    ref mask);
                ret |= DrawEquipSlotWithCheck(EquipSlot.Body,    equip.Body,    CharacterEquipMask.Body,    ref mask);
                ret |= DrawEquipSlotWithCheck(EquipSlot.Hands,   equip.Hands,   CharacterEquipMask.Hands,   ref mask);
                ret |= DrawEquipSlotWithCheck(EquipSlot.Legs,    equip.Legs,    CharacterEquipMask.Legs,    ref mask);
                ret |= DrawEquipSlotWithCheck(EquipSlot.Feet,    equip.Feet,    CharacterEquipMask.Feet,    ref mask);
                ret |= DrawEquipSlotWithCheck(EquipSlot.Ears,    equip.Ears,    CharacterEquipMask.Ears,    ref mask);
                ret |= DrawEquipSlotWithCheck(EquipSlot.Neck,    equip.Neck,    CharacterEquipMask.Neck,    ref mask);
                ret |= DrawEquipSlotWithCheck(EquipSlot.Wrists,  equip.Wrists,  CharacterEquipMask.Wrists,  ref mask);
                ret |= DrawEquipSlotWithCheck(EquipSlot.RFinger, equip.RFinger, CharacterEquipMask.RFinger, ref mask);
                ret |= DrawEquipSlotWithCheck(EquipSlot.LFinger, equip.LFinger, CharacterEquipMask.LFinger, ref mask);
            }

            return ret;
        }
    }
}
