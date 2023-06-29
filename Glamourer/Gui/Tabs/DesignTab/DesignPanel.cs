using System.Numerics;
using Dalamud.Interface;
using Glamourer.Designs;
using Glamourer.Gui.Customization;
using Glamourer.Gui.Equipment;
using Glamourer.Interop;
using Glamourer.State;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
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

    private void DrawHeader()
    {
        var buttonColor = ImGui.GetColorU32(ImGuiCol.FrameBg);
        var frameHeight = ImGui.GetFrameHeightWithSpacing();
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
            .Push(ImGuiStyleVar.FrameRounding, 0);
        ImGuiUtil.DrawTextButton(SelectionName, new Vector2(-frameHeight, ImGui.GetFrameHeight()), buttonColor);
        ImGui.SameLine();
        style.Push(ImGuiStyleVar.FrameBorderSize, ImGuiHelpers.GlobalScale);
        using var color = ImRaii.PushColor(ImGuiCol.Text, ColorId.FolderExpanded.Value())
            .Push(ImGuiCol.Border, ColorId.FolderExpanded.Value());
        if (ImGuiUtil.DrawDisabledButton(
                $"{(_selector.IncognitoMode ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash).ToIconString()}###IncognitoMode",
                new Vector2(frameHeight, ImGui.GetFrameHeight()), string.Empty, false, true))
            _selector.IncognitoMode = !_selector.IncognitoMode;
        var hovered = ImGui.IsItemHovered();
        color.Pop(2);
        if (hovered)
            ImGui.SetTooltip(_selector.IncognitoMode ? "Toggle incognito mode off." : "Toggle incognito mode on.");
    }

    private string SelectionName
        => _selector.Selected == null ? "No Selection" : _selector.IncognitoMode ? _selector.Selected.Incognito : _selector.Selected.Name.Text;

    public void Draw()
    {
        using var group  = ImRaii.Group();
        DrawHeader();
        
        var       design = _selector.Selected;
        using var child  = ImRaii.Child("##Panel", -Vector2.One, true);
        if (!child || design == null)
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
