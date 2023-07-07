using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Internal.Notifications;
using Glamourer.Automation;
using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.Gui.Customization;
using Glamourer.Gui.Equipment;
using Glamourer.Interop;
using Glamourer.Interop.Penumbra;
using Glamourer.Services;
using Glamourer.State;
using Glamourer.Structs;
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
    private readonly CustomizationService     _customizationService;
    private readonly ModAssociationsTab       _modAssociations;
    private readonly DesignDetailTab          _designDetails;
    private readonly DesignConverter          _converter;

    public DesignPanel(DesignFileSystemSelector selector, CustomizationDrawer customizationDrawer, DesignManager manager, ObjectManager objects,
        StateManager state, EquipmentDrawer equipmentDrawer, CustomizationService customizationService, PenumbraService penumbra,
        ModAssociationsTab modAssociations, DesignDetailTab designDetails, DesignConverter converter)
    {
        _selector             = selector;
        _customizationDrawer  = customizationDrawer;
        _manager              = manager;
        _objects              = objects;
        _state                = state;
        _equipmentDrawer      = equipmentDrawer;
        _customizationService = customizationService;
        _modAssociations      = modAssociations;
        _designDetails        = designDetails;
        _converter            = converter;
    }

    private void DrawHeader()
    {
        var selection   = _selector.Selected;
        var buttonColor = ImGui.GetColorU32(ImGuiCol.FrameBg);
        var frameHeight = ImGui.GetFrameHeightWithSpacing();
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
            .Push(ImGuiStyleVar.FrameRounding, 0);
        ImGuiUtil.DrawTextButton(SelectionName, new Vector2(selection != null ? -2 * frameHeight : -frameHeight, ImGui.GetFrameHeight()),
            buttonColor);

        ImGui.SameLine();
        style.Push(ImGuiStyleVar.FrameBorderSize, ImGuiHelpers.GlobalScale);
        using var color = ImRaii.PushColor(ImGuiCol.Border, ColorId.HeaderButtons.Value())
            .Push(ImGuiCol.Text, ColorId.HeaderButtons.Value());

        var hoverText = string.Empty;
        if (selection != null)
        {
            if (ImGuiUtil.DrawDisabledButton(
                    $"{(selection.WriteProtected() ? FontAwesomeIcon.LockOpen : FontAwesomeIcon.Lock).ToIconString()}###Locked",
                    new Vector2(frameHeight, ImGui.GetFrameHeight()), string.Empty, false, true))
                _manager.SetWriteProtection(selection, !selection.WriteProtected());
            if (ImGui.IsItemHovered())
                hoverText = selection.WriteProtected() ? "Make this design editable." : "Write-protect this design.";
        }

        ImGui.SameLine();
        if (ImGuiUtil.DrawDisabledButton(
                $"{(_selector.IncognitoMode ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash).ToIconString()}###IncognitoMode",
                new Vector2(frameHeight, ImGui.GetFrameHeight()), string.Empty, false, true))
            _selector.IncognitoMode = !_selector.IncognitoMode;
        if (ImGui.IsItemHovered())
            hoverText = _selector.IncognitoMode ? "Toggle incognito mode off." : "Toggle incognito mode on.";

        if (hoverText.Length > 0)
            ImGui.SetTooltip(hoverText);
    }

    private string SelectionName
        => _selector.Selected == null ? "No Selection" : _selector.IncognitoMode ? _selector.Selected.Incognito : _selector.Selected.Name.Text;

    private void DrawMetaData()
    {
        if (!ImGui.CollapsingHeader("MetaData"))
            return;

        using (var group1 = ImRaii.Group())
        {
            var apply = _selector.Selected!.DesignData.IsHatVisible();
            if (ImGui.Checkbox("Hat Visible", ref apply))
                _manager.ChangeMeta(_selector.Selected, ActorState.MetaIndex.HatState, apply);

            apply = _selector.Selected.DesignData.IsWeaponVisible();
            if (ImGui.Checkbox("Weapon Visible", ref apply))
                _manager.ChangeMeta(_selector.Selected, ActorState.MetaIndex.WeaponState, apply);
        }

        ImGui.SameLine(ImGui.GetContentRegionAvail().X / 2);

        using (var group2 = ImRaii.Group())
        {
            var apply = _selector.Selected.DesignData.IsVisorToggled();
            if (ImGui.Checkbox("Visor Toggled", ref apply))
                _manager.ChangeMeta(_selector.Selected, ActorState.MetaIndex.VisorState, apply);

            apply = _selector.Selected.DesignData.IsWet();
            if (ImGui.Checkbox("Force Wetness", ref apply))
                _manager.ChangeMeta(_selector.Selected, ActorState.MetaIndex.Wetness, apply);
        }
    }

    private void DrawEquipment()
    {
        if (!ImGui.CollapsingHeader("Equipment"))
            return;

        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var stain = _selector.Selected!.DesignData.Stain(slot);
            if (_equipmentDrawer.DrawStain(stain, slot, out var newStain))
                _manager.ChangeStain(_selector.Selected!, slot, newStain.RowIndex);

            ImGui.SameLine();
            var armor = _selector.Selected!.DesignData.Item(slot);
            if (_equipmentDrawer.DrawArmor(armor, slot, out var newArmor, _selector.Selected!.DesignData.Customize.Gender,
                    _selector.Selected!.DesignData.Customize.Race))
                _manager.ChangeEquip(_selector.Selected!, slot, newArmor);
        }

        var mhStain = _selector.Selected!.DesignData.Stain(EquipSlot.MainHand);
        if (_equipmentDrawer.DrawStain(mhStain, EquipSlot.MainHand, out var newMhStain))
            _manager.ChangeStain(_selector.Selected!, EquipSlot.MainHand, newMhStain.RowIndex);

        ImGui.SameLine();
        var mh = _selector.Selected!.DesignData.Item(EquipSlot.MainHand);
        if (_equipmentDrawer.DrawMainhand(mh, true, out var newMh))
            _manager.ChangeWeapon(_selector.Selected!, EquipSlot.MainHand, newMh);

        if (newMh.Type.Offhand() is not FullEquipType.Unknown)
        {
            var ohStain = _selector.Selected!.DesignData.Stain(EquipSlot.OffHand);
            if (_equipmentDrawer.DrawStain(ohStain, EquipSlot.OffHand, out var newOhStain))
                _manager.ChangeStain(_selector.Selected!, EquipSlot.OffHand, newOhStain.RowIndex);

            ImGui.SameLine();
            var oh = _selector.Selected!.DesignData.Item(EquipSlot.OffHand);
            if (_equipmentDrawer.DrawMainhand(oh, false, out var newOh))
                _manager.ChangeWeapon(_selector.Selected!, EquipSlot.OffHand, newOh);
        }
    }

    private void DrawCustomize()
    {
        if (ImGui.CollapsingHeader("Customization"))
            _customizationDrawer.Draw(_selector.Selected!.DesignData.Customize, _selector.Selected!.WriteProtected());
    }

    private void DrawApplicationRules()
    {
        if (!ImGui.CollapsingHeader("Application Rules"))
            return;

        using (var group1 = ImRaii.Group())
        {
            var set = _customizationService.AwaitedService.GetList(_selector.Selected!.DesignData.Customize.Clan,
                _selector.Selected!.DesignData.Customize.Gender);
            var all   = CustomizationExtensions.All.Where(set.IsAvailable).Select(c => c.ToFlag()).Aggregate((a, b) => a | b);
            var flags = (_selector.Selected!.ApplyCustomize & all) == 0 ? 0 : (_selector.Selected!.ApplyCustomize & all) == all ? 3 : 1;
            if (ImGui.CheckboxFlags("Apply All Customizations", ref flags, 3))
            {
                var newFlags = flags == 3;
                foreach (var index in CustomizationExtensions.All)
                    _manager.ChangeApplyCustomize(_selector.Selected!, index, newFlags);
            }

            var applyClan = _selector.Selected!.DoApplyCustomize(CustomizeIndex.Clan);
            if (ImGui.Checkbox($"Apply {CustomizeIndex.Clan.ToDefaultName()}", ref applyClan))
                _manager.ChangeApplyCustomize(_selector.Selected!, CustomizeIndex.Clan, applyClan);

            var applyGender = _selector.Selected!.DoApplyCustomize(CustomizeIndex.Gender);
            if (ImGui.Checkbox($"Apply {CustomizeIndex.Gender.ToDefaultName()}", ref applyGender))
                _manager.ChangeApplyCustomize(_selector.Selected!, CustomizeIndex.Gender, applyGender);


            foreach (var index in CustomizationExtensions.All.Where(set.IsAvailable))
            {
                var apply = _selector.Selected!.DoApplyCustomize(index);
                if (ImGui.Checkbox($"Apply {index.ToDefaultName()}", ref apply))
                    _manager.ChangeApplyCustomize(_selector.Selected!, index, apply);
            }
        }

        ImGui.SameLine(ImGui.GetContentRegionAvail().X / 2);
        using (var group2 = ImRaii.Group())
        {
            void ApplyEquip(string label, EquipFlag all, bool stain, IEnumerable<EquipSlot> slots)
            {
                var flags = (uint)(all & _selector.Selected!.ApplyEquip);

                var bigChange = ImGui.CheckboxFlags($"Apply All {label}", ref flags, (uint)all);
                if (stain)
                    foreach (var slot in slots)
                    {
                        var apply = bigChange ? ((EquipFlag)flags).HasFlag(slot.ToStainFlag()) : _selector.Selected!.DoApplyStain(slot);
                        if (ImGui.Checkbox($"Apply {slot.ToName()} Dye", ref apply) || bigChange)
                            _manager.ChangeApplyStain(_selector.Selected!, slot, apply);
                    }
                else
                    foreach (var slot in slots)
                    {
                        var apply = bigChange ? ((EquipFlag)flags).HasFlag(slot.ToFlag()) : _selector.Selected!.DoApplyEquip(slot);
                        if (ImGui.Checkbox($"Apply {slot.ToName()}", ref apply) || bigChange)
                            _manager.ChangeApplyEquip(_selector.Selected!, slot, apply);
                    }
            }

            ApplyEquip("Weapons", AutoDesign.WeaponFlags, false, new[]
            {
                EquipSlot.MainHand,
                EquipSlot.OffHand,
            });

            ImGui.NewLine();
            ApplyEquip("Armor", AutoDesign.ArmorFlags, false, EquipSlotExtensions.EquipmentSlots);

            ImGui.NewLine();
            ApplyEquip("Accessories", AutoDesign.AccessoryFlags, false, EquipSlotExtensions.AccessorySlots);

            ImGui.NewLine();
            ApplyEquip("Dyes", AutoDesign.StainFlags, true,
                EquipSlotExtensions.EqdpSlots.Prepend(EquipSlot.MainHand).Prepend(EquipSlot.OffHand));

            ImGui.NewLine();
            const uint all = 0x0Fu;
            var flags = (_selector.Selected!.DoApplyHatVisible() ? 0x01u : 0x00)
              | (_selector.Selected!.DoApplyVisorToggle() ? 0x02u : 0x00)
              | (_selector.Selected!.DoApplyWeaponVisible() ? 0x04u : 0x00)
              | (_selector.Selected!.DoApplyWetness() ? 0x08u : 0x00);
            var bigChange = ImGui.CheckboxFlags("Apply All Meta Changes", ref flags, all);
            var apply     = bigChange ? (flags & 0x01) == 0x01 : _selector.Selected!.DoApplyHatVisible();
            if (ImGui.Checkbox("Apply Hat Visibility", ref apply) || bigChange)
                _manager.ChangeApplyMeta(_selector.Selected!, ActorState.MetaIndex.HatState, apply);

            apply = bigChange ? (flags & 0x02) == 0x02 : _selector.Selected!.DoApplyVisorToggle();
            if (ImGui.Checkbox("Apply Visor State", ref apply) || bigChange)
                _manager.ChangeApplyMeta(_selector.Selected!, ActorState.MetaIndex.VisorState, apply);

            apply = bigChange ? (flags & 0x04) == 0x04 : _selector.Selected!.DoApplyWeaponVisible();
            if (ImGui.Checkbox("Apply Weapon Visibility", ref apply) || bigChange)
                _manager.ChangeApplyMeta(_selector.Selected!, ActorState.MetaIndex.WeaponState, apply);

            apply = bigChange ? (flags & 0x08) == 0x08 : _selector.Selected!.DoApplyWetness();
            if (ImGui.Checkbox("Apply Wetness", ref apply) || bigChange)
                _manager.ChangeApplyMeta(_selector.Selected!, ActorState.MetaIndex.Wetness, apply);
        }
    }

    public void Draw()
    {
        using var group = ImRaii.Group();
        DrawHeader();

        var       design = _selector.Selected;
        using var child  = ImRaii.Child("##Panel", -Vector2.One, true);
        if (!child || design == null)
            return;

        DrawButtonRow();
        DrawMetaData();
        DrawCustomize();
        DrawEquipment();
        _designDetails.Draw();
        DrawApplicationRules();
        _modAssociations.Draw();
    }

    private void DrawButtonRow()
    {
        DrawSetFromClipboard();
        ImGui.SameLine();
        DrawExportToClipboard();
        ImGui.SameLine();
        DrawApplyToSelf();
        ImGui.SameLine();
        DrawApplyToTarget();
    }

    private void DrawSetFromClipboard()
    {
        if (!ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Clipboard.ToIconString(), new Vector2(ImGui.GetFrameHeight()),
                "Try to apply a design from your clipboard.", _selector.Selected!.WriteProtected(), true))
            return;

        try
        {
            var text   = ImGui.GetClipboardText();
            var design = _converter.FromBase64(text, true, true) ?? throw new Exception("The clipboard did not contain valid data.");
            _manager.ApplyDesign(_selector.Selected!, design);
        }
        catch (Exception ex)
        {
            Glamourer.Chat.NotificationMessage(ex, $"Could not apply clipboard to {_selector.Selected!.Name}.",
                $"Could not apply clipboard to design {_selector.Selected!.Identifier}", "Failure", NotificationType.Error);
        }
    }

    private void DrawExportToClipboard()
    {
        if (!ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Copy.ToIconString(), new Vector2(ImGui.GetFrameHeight()),
                "Copy the current design to your clipboard.", false, true))
            return;

        try
        {
            var text = _converter.ShareBase64(_selector.Selected!);
            ImGui.SetClipboardText(text);
        }
        catch (Exception ex)
        {
            Glamourer.Chat.NotificationMessage(ex, $"Could not copy {_selector.Selected!.Name} data to clipboard.",
                $"Could not copy data from design {_selector.Selected!.Identifier} to clipboard", "Failure", NotificationType.Error);
        }
    }

    private void DrawApplyToSelf()
    {
        var (id, data) = _objects.PlayerData;
        if (!ImGuiUtil.DrawDisabledButton("Apply to Yourself", Vector2.Zero, "Apply the current design with its settings to your character.",
                !data.Valid))
            return;

        if (_state.GetOrCreate(id, data.Objects[0], out var state))
            _state.ApplyDesign(_selector.Selected!, state);
    }

    private void DrawApplyToTarget()
    {
        var (id, data) = _objects.TargetData;
        var tt = id.IsValid
            ? data.Valid
                ? "Apply the current design with its settings to your current target."
                : "The current target can not be manipulated."
            : "No valid target selected.";
        if (!ImGuiUtil.DrawDisabledButton("Apply to Target", Vector2.Zero, tt, !data.Valid))
            return;

        if (_state.GetOrCreate(id, data.Objects[0], out var state))
            _state.ApplyDesign(_selector.Selected!, state);
    }
}
