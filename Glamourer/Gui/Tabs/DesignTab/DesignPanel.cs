using System.Numerics;
using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.Gui.Customization;
using Glamourer.Gui.Equipment;
using Glamourer.Interop;
using Glamourer.Interop.Penumbra;
using Glamourer.State;
using Glamourer.Structs;
using ImGuiNET;
using OtterGui.Raii;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;
using Penumbra.GameData.Enums;

namespace Glamourer.Gui.Tabs.DesignTab;

public class DesignPanel
{
    private readonly ObjectManager            _objects;
    private readonly DesignFileSystemSelector _selector;
    private readonly DesignManager            _manager;
    private readonly CustomizationDrawer      _customizationDrawer;
    private readonly StateManager             _state;
    private readonly EquipmentDrawer          _equipmentDrawer;

    public DesignPanel(DesignFileSystemSelector selector, CustomizationDrawer customizationDrawer, DesignManager manager, ObjectManager objects,
        StateManager state, EquipmentDrawer equipmentDrawer)
    {
        _selector            = selector;
        _customizationDrawer = customizationDrawer;
        _manager             = manager;
        _objects             = objects;
        _state               = state;
        _equipmentDrawer     = equipmentDrawer;
    }

    public void Draw()
    {
        var design = _selector.Selected;
        if (design == null)
            return;

        using var child = ImRaii.Child("##panel", -Vector2.One, true);
        if (!child)
            return;

        if (ImGui.Button("TEST"))
        {
            var (id, data) = _objects.PlayerData;

            if (data.Valid && _state.GetOrCreate(id, data.Objects[0], out var state))
                _state.ApplyDesign(design, state);
        }

        _customizationDrawer.Draw(design.DesignData.Customize, design.WriteProtected());

        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var stain = design.DesignData.Stain(slot);
            if (_equipmentDrawer.DrawStain(stain, slot, out var newStain))
                _manager.ChangeStain(design, slot, newStain.RowIndex);

            ImGui.SameLine();
            var armor = design.DesignData.Item(slot);
            if (_equipmentDrawer.DrawArmor(armor, slot, out var newArmor, design.DesignData.Customize.Gender, design.DesignData.Customize.Race))
                _manager.ChangeEquip(design, slot, newArmor);
        }

        var mhStain = design.DesignData.Stain(EquipSlot.MainHand);
        if (_equipmentDrawer.DrawStain(mhStain, EquipSlot.MainHand, out var newMhStain))
            _manager.ChangeStain(design, EquipSlot.MainHand, newMhStain.RowIndex);

        ImGui.SameLine();
        var mh = design.DesignData.Item(EquipSlot.MainHand);
        if (_equipmentDrawer.DrawMainhand(mh, true, out var newMh))
            _manager.ChangeWeapon(design, EquipSlot.MainHand, newMh);

        if (newMh.Type.Offhand() is not FullEquipType.Unknown)
        {
            var ohStain = design.DesignData.Stain(EquipSlot.OffHand);
            if (_equipmentDrawer.DrawStain(ohStain, EquipSlot.OffHand, out var newOhStain))
                _manager.ChangeStain(design, EquipSlot.OffHand, newOhStain.RowIndex);

            ImGui.SameLine();
            var oh = design.DesignData.Item(EquipSlot.OffHand);
            if (_equipmentDrawer.DrawMainhand(oh, false, out var newOh))
                _manager.ChangeWeapon(design, EquipSlot.OffHand, newOh);
        }
    }
}
