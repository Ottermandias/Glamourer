using System;
using System.Collections.Generic;
using System.Linq;
using Glamourer.Customization;
using Glamourer.Interop;
using ImGuiNET;
using OtterGui.Raii;
using OtterGui.Widgets;
using Penumbra.GameData.Data;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Equipment;

public enum ApplicationFlags
{ }

public partial class EquipmentDrawer
{
    private static readonly FilterComboColors _stainCombo;
    private static readonly StainData         _stainData;

    private Race                       _race;
    private Gender                     _gender;
    private CharacterEquip             _equip;
    private IReadOnlyCollection<Actor> _actors = Array.Empty<Actor>();

    private CharacterArmor _currentArmor;
    private EquipSlot      _currentSlot;
    private uint           _currentSlotIdx;

    static EquipmentDrawer()
    {
        _stainData = new StainData(Dalamud.PluginInterface, Dalamud.GameData, Dalamud.GameData.Language);
        _stainCombo = new FilterComboColors(140,
            _stainData.Data.Prepend(new KeyValuePair<byte, (string Name, uint Dye, bool Gloss)>(0, ("None", 0, false))));
        Identifier    = Glamourer.Identifier;
        ItemCombos    = EquipSlotExtensions.EqdpSlots.Select(s => new ItemCombo(s)).ToArray();
        MainHandCombo = new WeaponCombo(EquipSlot.MainHand);
        OffHandCombo  = new WeaponCombo(EquipSlot.OffHand);
    }

    public static void Draw(Customize customize, CharacterEquip equip, ref CharacterWeapon mainHand, ref CharacterWeapon offHand,
        IReadOnlyCollection<Actor> actors, bool locked)
    {
        var d = new EquipmentDrawer()
        {
            _race   = customize.Race,
            _gender = customize.Gender,
            _equip  = equip,
            _actors = actors,
        };

        if (!ImGui.CollapsingHeader("Character Equipment"))
            return;

        using var disabled = ImRaii.Disabled(locked);

        d.DrawInternal(ref mainHand, ref offHand);
    }


    public static void Draw(Customize customize, CharacterEquip equip, ref CharacterWeapon mainHand, ref CharacterWeapon offHand,
        ref ApplicationFlags flags, IReadOnlyCollection<Actor> actors,
        bool locked)
    {
        var d = new EquipmentDrawer()
        {
            _race   = customize.Race,
            _gender = customize.Gender,
            _equip  = equip,
            _actors = actors,
        };

        if (!ImGui.CollapsingHeader("Character Equipment"))
            return;

        using var disabled = ImRaii.Disabled(locked);

        d.DrawInternal(ref mainHand, ref offHand, ref flags);
    }

    private void DrawStainCombo()
    {
        var found = _stainData.TryGetValue(_currentArmor.Stain, out var stain);
        _stainCombo.Draw("##stain", stain.RgbaColor, found);
    }

    private void DrawInternal(ref CharacterWeapon mainHand, ref CharacterWeapon offHand)
    {
        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            using var id = SetSlot(slot);
            DrawStainCombo();
            ImGui.SameLine();
            DrawItemSelector();
        }

        _currentSlot = EquipSlot.MainHand;
        DrawStainCombo();
        ImGui.SameLine();
        DrawMainHandSelector(ref mainHand);
        var offhand = MainHandCombo.LastCategory.AllowsOffHand();
        if (offhand != WeaponCategory.Unknown)
        {
            _currentSlot = EquipSlot.OffHand;
            DrawStainCombo();
            ImGui.SameLine();
            DrawOffHandSelector(ref offHand, offhand);
        }
    }

    private void DrawInternal(ref CharacterWeapon mainHand, ref CharacterWeapon offHand, ref ApplicationFlags flags)
    {
        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            using var id = SetSlot(slot);
            DrawCheckbox(ref flags);
            ImGui.SameLine();
            DrawStainCombo();
            ImGui.SameLine();
            DrawItemSelector();
        }

        _currentSlot = EquipSlot.MainHand;
        DrawCheckbox(ref flags);
        ImGui.SameLine();
        DrawStainCombo();
        ImGui.SameLine();
        DrawMainHandSelector(ref mainHand);
        var offhand = MainHandCombo.LastCategory.AllowsOffHand();
        if (offhand != WeaponCategory.Unknown)
        {
            _currentSlot = EquipSlot.OffHand;
            DrawCheckbox(ref flags);
            ImGui.SameLine();
            DrawStainCombo();
            ImGui.SameLine();
            DrawOffHandSelector(ref offHand, offhand);
        }
    }

    private ImRaii.Id SetSlot(EquipSlot slot)
    {
        _currentSlot    = slot;
        _currentSlotIdx = slot.ToIndex();
        _currentArmor   = _equip[slot];
        return ImRaii.PushId((int)slot);
    }

    private void UpdateActors()
    {
        _equip[_currentSlotIdx] = _currentArmor;
        foreach (var actor in _actors)
            Glamourer.RedrawManager.ChangeEquip(actor, _currentSlotIdx, _currentArmor);
    }
}
