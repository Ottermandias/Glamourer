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

            if (stainCombo.Draw(string.Empty, out var newStain) && _player != null && !newStain.RowIndex.Equals(stainIdx))
            {
                newStain.Write(_player.Address, slot);
                return true;
            }

            return false;
        }

        private bool DrawItemSelector(ComboWithFilter<Item> equipCombo, Lumina.Excel.GeneratedSheets.Item? item)
        {
            var currentName = item?.Name.ToString() ?? "Nothing";
            if (equipCombo.Draw(currentName, out var newItem, _itemComboWidth) && _player != null && newItem.Base.RowId != item?.RowId)
            {
                newItem.Write(_player.Address);
                return true;
            }

            return false;
        }

        private static bool DrawCheckbox(ActorEquipMask flag, ref ActorEquipMask mask)
        {
            var tmp = (uint) mask;
            var ret = false;
            if (ImGui.CheckboxFlags($"##flag_{(uint) flag}", ref tmp, (uint) flag) && tmp != (uint) mask)
            {
                mask = (ActorEquipMask) tmp;
                ret  = true;
            }

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Enable writing this slot in this save.");
            return ret;
        }

        private bool DrawEquipSlot(EquipSlot slot, ActorArmor equip)
        {
            var (equipCombo, stainCombo) = _combos[slot];

            var ret = DrawStainSelector(stainCombo, slot, equip.Stain);
            ImGui.SameLine();
            var item = _identifier.Identify(equip.Set, new WeaponType(), equip.Variant, slot);
            ret |= DrawItemSelector(equipCombo, item);

            return ret;
        }

        private bool DrawEquipSlotWithCheck(EquipSlot slot, ActorArmor equip, ActorEquipMask flag, ref ActorEquipMask mask)
        {
            var ret = DrawCheckbox(flag, ref mask);
            ImGui.SameLine();
            ret |= DrawEquipSlot(slot, equip);
            return ret;
        }

        private bool DrawWeapon(EquipSlot slot, ActorWeapon weapon)
        {
            var (equipCombo, stainCombo) = _combos[slot];

            var ret = DrawStainSelector(stainCombo, slot, weapon.Stain);
            ImGui.SameLine();
            var item = _identifier.Identify(weapon.Set, weapon.Type, weapon.Variant, slot);
            ret |= DrawItemSelector(equipCombo, item);

            return ret;
        }

        private bool DrawWeaponWithCheck(EquipSlot slot, ActorWeapon weapon, ActorEquipMask flag, ref ActorEquipMask mask)
        {
            var ret = DrawCheckbox(flag, ref mask);
            ImGui.SameLine();
            ret |= DrawWeapon(slot, weapon);
            return ret;
        }

        private bool DrawEquip(ActorEquipment equip)
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

        private bool DrawEquip(ActorEquipment equip, ref ActorEquipMask mask)
        {
            var ret = false;
            if (ImGui.CollapsingHeader("Character Equipment"))
            {
                ret |= DrawWeaponWithCheck(EquipSlot.MainHand, equip.MainHand, ActorEquipMask.MainHand, ref mask);
                ret |= DrawWeaponWithCheck(EquipSlot.OffHand,  equip.OffHand,  ActorEquipMask.OffHand,  ref mask);
                ret |= DrawEquipSlotWithCheck(EquipSlot.Head,    equip.Head,    ActorEquipMask.Head,    ref mask);
                ret |= DrawEquipSlotWithCheck(EquipSlot.Body,    equip.Body,    ActorEquipMask.Body,    ref mask);
                ret |= DrawEquipSlotWithCheck(EquipSlot.Hands,   equip.Hands,   ActorEquipMask.Hands,   ref mask);
                ret |= DrawEquipSlotWithCheck(EquipSlot.Legs,    equip.Legs,    ActorEquipMask.Legs,    ref mask);
                ret |= DrawEquipSlotWithCheck(EquipSlot.Feet,    equip.Feet,    ActorEquipMask.Feet,    ref mask);
                ret |= DrawEquipSlotWithCheck(EquipSlot.Ears,    equip.Ears,    ActorEquipMask.Ears,    ref mask);
                ret |= DrawEquipSlotWithCheck(EquipSlot.Neck,    equip.Neck,    ActorEquipMask.Neck,    ref mask);
                ret |= DrawEquipSlotWithCheck(EquipSlot.Wrists,  equip.Wrists,  ActorEquipMask.Wrists,  ref mask);
                ret |= DrawEquipSlotWithCheck(EquipSlot.RFinger, equip.RFinger, ActorEquipMask.RFinger, ref mask);
                ret |= DrawEquipSlotWithCheck(EquipSlot.LFinger, equip.LFinger, ActorEquipMask.LFinger, ref mask);
            }

            return ret;
        }
    }
}
