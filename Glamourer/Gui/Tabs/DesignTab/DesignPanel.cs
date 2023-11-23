﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Xml.Linq;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Internal.Notifications;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Glamourer.Automation;
using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Gui.Customization;
using Glamourer.Gui.Equipment;
using Glamourer.Interop;
using Glamourer.State;
using Glamourer.Structs;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;
using Penumbra.GameData.Enums;

namespace Glamourer.Gui.Tabs.DesignTab;

public class DesignPanel(DesignFileSystemSelector _selector, CustomizationDrawer _customizationDrawer, DesignManager _manager,
    ObjectManager _objects, StateManager _state, EquipmentDrawer _equipmentDrawer, ModAssociationsTab _modAssociations,
    DesignDetailTab _designDetails, DesignConverter _converter, ImportService _importService, MultiDesignPanel _multiDesignPanel)
{
    private static readonly EquipSlot[] CrestSlots = EquipSlotExtensions.FullSlots.Where(EquipmentDrawer.CanHaveCrest).ToArray();

    private readonly FileDialogManager _fileDialog = new();

    private HeaderDrawer.Button LockButton()
        => _selector.Selected == null
            ? HeaderDrawer.Button.Invisible
            : _selector.Selected.WriteProtected()
                ? new HeaderDrawer.Button
                {
                    Description = "Make this design editable.",
                    Icon        = FontAwesomeIcon.Lock,
                    OnClick     = () => _manager.SetWriteProtection(_selector.Selected!, false),
                }
                : new HeaderDrawer.Button
                {
                    Description = "Write-protect this design.",
                    Icon        = FontAwesomeIcon.LockOpen,
                    OnClick     = () => _manager.SetWriteProtection(_selector.Selected!, true),
                };

    private HeaderDrawer.Button SetFromClipboardButton()
        => new()
        {
            Description =
                "Try to apply a design from your clipboard over this design.\nHold Control to only apply gear.\nHold Shift to only apply customizations.",
            Icon     = FontAwesomeIcon.Clipboard,
            OnClick  = SetFromClipboard,
            Visible  = _selector.Selected != null,
            Disabled = _selector.Selected?.WriteProtected() ?? true,
        };

    private HeaderDrawer.Button UndoButton()
        => new()
        {
            Description = "Undo the last change if you accidentally overwrote your design with a different one.",
            Icon        = FontAwesomeIcon.Undo,
            OnClick     = UndoOverwrite,
            Visible     = _selector.Selected != null,
            Disabled    = !_manager.CanUndo(_selector.Selected),
        };

    private HeaderDrawer.Button ExportToClipboardButton()
        => new()
        {
            Description = "Copy the current design to your clipboard.",
            Icon        = FontAwesomeIcon.Copy,
            OnClick     = ExportToClipboard,
            Visible     = _selector.Selected != null,
        };

    private void DrawHeader()
        => HeaderDrawer.Draw(SelectionName, 0, ImGui.GetColorU32(ImGuiCol.FrameBg),
            3, SetFromClipboardButton(), UndoButton(), ExportToClipboardButton(), LockButton(),
            HeaderDrawer.Button.IncognitoButton(_selector.IncognitoMode, v => _selector.IncognitoMode = v));

    private string SelectionName
        => _selector.Selected == null ? "No Selection" : _selector.IncognitoMode ? _selector.Selected.Incognito : _selector.Selected.Name.Text;

    private void DrawEquipment()
    {
        if (!ImGui.CollapsingHeader("Equipment"))
            return;

        _equipmentDrawer.Prepare();

        var usedAllStain = _equipmentDrawer.DrawAllStain(out var newAllStain, _selector.Selected!.WriteProtected());
        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var changes = _equipmentDrawer.DrawEquip(slot, _selector.Selected!.DesignData, out var newArmor, out var newStain, out var newCrest,
                _selector.Selected.ApplyEquip, out var newApply, out var newApplyStain, out var newApplyCrest, _selector.Selected!.WriteProtected());
            if (changes.HasFlag(DataChange.Item))
                _manager.ChangeEquip(_selector.Selected, slot, newArmor);
            if (changes.HasFlag(DataChange.Stain))
                _manager.ChangeStain(_selector.Selected, slot, newStain);
            else if (usedAllStain)
                _manager.ChangeStain(_selector.Selected, slot, newAllStain);
            if (changes.HasFlag(DataChange.Crest))
                _manager.ChangeCrest(_selector.Selected, slot, newCrest);
            if (changes.HasFlag(DataChange.ApplyItem))
                _manager.ChangeApplyEquip(_selector.Selected, slot, newApply);
            if (changes.HasFlag(DataChange.ApplyStain))
                _manager.ChangeApplyStain(_selector.Selected, slot, newApplyStain);
            if (changes.HasFlag(DataChange.ApplyCrest))
                _manager.ChangeApplyCrest(_selector.Selected, slot, newApplyCrest);
        }

        var weaponChanges = _equipmentDrawer.DrawWeapons(_selector.Selected!.DesignData, out var newMainhand, out var newOffhand,
            out var newMainhandStain, out var newOffhandStain, out var newMainhandCrest, out var newOffhandCrest,
            _selector.Selected.ApplyEquip, true, out var applyMain, out var applyMainStain, out var applyMainCrest,
            out var applyOff, out var applyOffStain, out var applyOffCrest, _selector.Selected!.WriteProtected());

        if (weaponChanges.HasFlag(DataChange.Item))
            _manager.ChangeWeapon(_selector.Selected, EquipSlot.MainHand, newMainhand);
        if (weaponChanges.HasFlag(DataChange.Stain))
            _manager.ChangeStain(_selector.Selected, EquipSlot.MainHand, newMainhandStain);
        else if (usedAllStain)
            _manager.ChangeStain(_selector.Selected, EquipSlot.MainHand, newAllStain);
        if (weaponChanges.HasFlag(DataChange.Crest))
            _manager.ChangeCrest(_selector.Selected, EquipSlot.MainHand, newMainhandCrest);
        if (weaponChanges.HasFlag(DataChange.ApplyItem))
            _manager.ChangeApplyEquip(_selector.Selected, EquipSlot.MainHand, applyMain);
        if (weaponChanges.HasFlag(DataChange.ApplyStain))
            _manager.ChangeApplyStain(_selector.Selected, EquipSlot.MainHand, applyMainStain);
        if (weaponChanges.HasFlag(DataChange.ApplyCrest))
            _manager.ChangeApplyCrest(_selector.Selected, EquipSlot.MainHand, applyMainCrest);
        if (weaponChanges.HasFlag(DataChange.Item2))
            _manager.ChangeWeapon(_selector.Selected, EquipSlot.OffHand, newOffhand);
        if (weaponChanges.HasFlag(DataChange.Stain2))
            _manager.ChangeStain(_selector.Selected, EquipSlot.OffHand, newOffhandStain);
        else if (usedAllStain)
            _manager.ChangeStain(_selector.Selected, EquipSlot.OffHand, newAllStain);
        if (weaponChanges.HasFlag(DataChange.Crest2))
            _manager.ChangeCrest(_selector.Selected, EquipSlot.OffHand, newOffhandCrest);
        if (weaponChanges.HasFlag(DataChange.ApplyItem2))
            _manager.ChangeApplyEquip(_selector.Selected, EquipSlot.OffHand, applyOff);
        if (weaponChanges.HasFlag(DataChange.ApplyStain2))
            _manager.ChangeApplyStain(_selector.Selected, EquipSlot.OffHand, applyOffStain);
        if (weaponChanges.HasFlag(DataChange.ApplyCrest2))
            _manager.ChangeApplyCrest(_selector.Selected, EquipSlot.OffHand, applyOffCrest);

        ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight() / 2));
        DrawEquipmentMetaToggles();
        ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight() / 2));
    }

    private void DrawEquipmentMetaToggles()
    {
        var hatChanges = EquipmentDrawer.DrawHatState(_selector.Selected!.DesignData.IsHatVisible(),
            _selector.Selected.DoApplyHatVisible(),
            out var newHatState, out var newHatApply, _selector.Selected.WriteProtected());
        ApplyChanges(ActorState.MetaIndex.HatState, hatChanges, newHatState, newHatApply);

        ImGui.SameLine();
        var visorChanges = EquipmentDrawer.DrawVisorState(_selector.Selected!.DesignData.IsVisorToggled(),
            _selector.Selected.DoApplyVisorToggle(),
            out var newVisorState, out var newVisorApply, _selector.Selected.WriteProtected());
        ApplyChanges(ActorState.MetaIndex.VisorState, visorChanges, newVisorState, newVisorApply);

        ImGui.SameLine();
        var weaponChanges = EquipmentDrawer.DrawWeaponState(_selector.Selected!.DesignData.IsWeaponVisible(),
            _selector.Selected.DoApplyWeaponVisible(),
            out var newWeaponState, out var newWeaponApply, _selector.Selected.WriteProtected());
        ApplyChanges(ActorState.MetaIndex.WeaponState, weaponChanges, newWeaponState, newWeaponApply);
    }

    private void DrawCustomize()
    {
        var header = _selector.Selected!.DesignData.ModelId == 0
            ? "Customization"
            : $"Customization (Model Id #{_selector.Selected!.DesignData.ModelId})###Customization";
        if (!ImGui.CollapsingHeader(header))
            return;

        if (_customizationDrawer.Draw(_selector.Selected!.DesignData.Customize, _selector.Selected.ApplyCustomizeRaw,
                _selector.Selected!.WriteProtected(), false))
            foreach (var idx in Enum.GetValues<CustomizeIndex>())
            {
                var flag     = idx.ToFlag();
                var newValue = _customizationDrawer.ChangeApply.HasFlag(flag);
                _manager.ChangeApplyCustomize(_selector.Selected, idx, newValue);
                if (_customizationDrawer.Changed.HasFlag(flag))
                    _manager.ChangeCustomize(_selector.Selected, idx, _customizationDrawer.Customize[idx]);
            }

        var wetnessChanges = _customizationDrawer.DrawWetnessState(_selector.Selected!.DesignData.IsWet(),
            _selector.Selected!.DoApplyWetness(), out var newWetnessState, out var newWetnessApply, _selector.Selected!.WriteProtected());
        ApplyChanges(ActorState.MetaIndex.Wetness, wetnessChanges, newWetnessState, newWetnessApply);
        ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight() / 2));
    }

    private void DrawApplicationRules()
    {
        if (!ImGui.CollapsingHeader("Application Rules"))
            return;

        using (var _ = ImRaii.Group())
        {
            var set       = _selector.Selected!.CustomizationSet;
            var available = set.SettingAvailable | CustomizeFlag.Clan | CustomizeFlag.Gender;
            var flags     = _selector.Selected!.ApplyCustomize == 0 ? 0 : (_selector.Selected!.ApplyCustomize & available) == available ? 3 : 1;
            if (ImGui.CheckboxFlags("Apply All Customizations", ref flags, 3))
            {
                var newFlags = flags == 3;
                _manager.ChangeApplyCustomize(_selector.Selected!, CustomizeIndex.Clan,   newFlags);
                _manager.ChangeApplyCustomize(_selector.Selected!, CustomizeIndex.Gender, newFlags);
                foreach (var index in CustomizationExtensions.AllBasic)
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
                if (ImGui.Checkbox($"Apply {set.Option(index)}", ref apply))
                    _manager.ChangeApplyCustomize(_selector.Selected!, index, apply);
            }
        }

        ImGui.SameLine(ImGui.GetContentRegionAvail().X / 2);
        using (var _ = ImRaii.Group())
        {
            void ApplyEquip(string label, EquipFlag allFlags, ActorState.EquipField equipField, IEnumerable<EquipSlot> slots)
            {
                // The flags we may edit with a single "big change" checkbox will always fit in a uint, but the whole bitfield doesn't
                var shift = (int)equipField;
                var flags = (uint)((ulong)(allFlags & _selector.Selected!.ApplyEquip) >> shift);

                var bigChange = ImGui.CheckboxFlags($"Apply All {label}", ref flags, (uint)((ulong)allFlags >> shift));

                var adjustedFlags = (EquipFlag)((ulong)flags << shift);
                switch (equipField)
                {
                    case ActorState.EquipField.Stain:
                        foreach (var slot in slots)
                        {
                            var apply = bigChange ? adjustedFlags.HasFlag(slot.ToStainFlag()) : _selector.Selected!.DoApplyStain(slot);
                            if (ImGui.Checkbox($"Apply {slot.ToName()} Dye", ref apply) || bigChange)
                                _manager.ChangeApplyStain(_selector.Selected!, slot, apply);
                        }
                        break;
                    case ActorState.EquipField.Crest:
                        foreach (var slot in slots)
                        {
                            var apply = bigChange ? adjustedFlags.HasFlag(slot.ToCrestFlag()) : _selector.Selected!.DoApplyCrest(slot);
                            if (ImGui.Checkbox($"Apply {slot.ToName()} Crest Visibility", ref apply) || bigChange)
                                _manager.ChangeApplyCrest(_selector.Selected!, slot, apply);
                        }
                        break;
                    default:
                        foreach (var slot in slots)
                        {
                            var apply = bigChange ? adjustedFlags.HasFlag(slot.ToFlag()) : _selector.Selected!.DoApplyEquip(slot);
                            if (ImGui.Checkbox($"Apply {slot.ToName()}", ref apply) || bigChange)
                                _manager.ChangeApplyEquip(_selector.Selected!, slot, apply);
                        }
                        break;
                }
            }

            ApplyEquip("Weapons", AutoDesign.WeaponFlags, ActorState.EquipField.Item, EquipSlotExtensions.WeaponSlots);

            ImGui.NewLine();
            ApplyEquip("Armor", AutoDesign.ArmorFlags, ActorState.EquipField.Item, EquipSlotExtensions.EquipmentSlots);

            ImGui.NewLine();
            ApplyEquip("Accessories", AutoDesign.AccessoryFlags, ActorState.EquipField.Item, EquipSlotExtensions.AccessorySlots);

            ImGui.NewLine();
            ApplyEquip("Dyes", AutoDesign.StainFlags, ActorState.EquipField.Stain,
                EquipSlotExtensions.FullSlots);

            ImGui.NewLine();
            ApplyEquip("Crest Visibilities", AutoDesign.RelevantCrestFlags, ActorState.EquipField.Crest, CrestSlots);

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
        if (_selector.SelectedPaths.Count > 1)
        {
            _multiDesignPanel.Draw();
        }
        else
        {
            DrawHeader();
            DrawPanel();

            if (_selector.Selected == null || _selector.Selected.WriteProtected())
                return;

            if (_importService.CreateDatTarget(out var dat))
            {
                _manager.ChangeCustomize(_selector.Selected!, CustomizeIndex.Clan,   dat.Customize[CustomizeIndex.Clan]);
                _manager.ChangeCustomize(_selector.Selected!, CustomizeIndex.Gender, dat.Customize[CustomizeIndex.Gender]);
                foreach (var idx in CustomizationExtensions.AllBasic)
                    _manager.ChangeCustomize(_selector.Selected!, idx, dat.Customize[idx]);
                Glamourer.Messager.NotificationMessage($"Applied games .dat file {dat.Description} customizations to {_selector.Selected.Name}.", NotificationType.Success, false);
            }
            else if (_importService.CreateCharaTarget(out var designBase, out var name))
            {
                _manager.ApplyDesign(_selector.Selected!, designBase);
                Glamourer.Messager.NotificationMessage($"Applied Anamnesis .chara file {name} to {_selector.Selected.Name}.", NotificationType.Success, false);
            }
        }

        _importService.CreateDatSource();
    }

    private void DrawPanel()
    {
        using var child = ImRaii.Child("##Panel", -Vector2.One, true);
        if (!child || _selector.Selected == null)
            return;

        DrawButtonRow();
        DrawCustomize();
        DrawEquipment();
        _designDetails.Draw();
        DrawApplicationRules();
        _modAssociations.Draw();
    }

    private void DrawButtonRow()
    {
        DrawApplyToSelf();
        ImGui.SameLine();
        DrawApplyToTarget();
        ImGui.SameLine();
        _modAssociations.DrawApplyButton();
        ImGui.SameLine();
        DrawSaveToDat();
    }

    private void SetFromClipboard()
    {
        try
        {
            var text = ImGui.GetClipboardText();
            var (applyEquip, applyCustomize) = UiHelpers.ConvertKeysToBool();
            var design = _converter.FromBase64(text, applyCustomize, applyEquip, out _)
             ?? throw new Exception("The clipboard did not contain valid data.");
            _manager.ApplyDesign(_selector.Selected!, design);
        }
        catch (Exception ex)
        {
            Glamourer.Messager.NotificationMessage(ex, $"Could not apply clipboard to {_selector.Selected!.Name}.",
                $"Could not apply clipboard to design {_selector.Selected!.Identifier}", NotificationType.Error, false);
        }
    }

    private void UndoOverwrite()
    {
        try
        {
            _manager.UndoDesignChange(_selector.Selected!);
        }
        catch (Exception ex)
        {
            Glamourer.Messager.NotificationMessage(ex, $"Could not undo last changes to {_selector.Selected!.Name}.", NotificationType.Error,
                false);
        }
    }

    private void ExportToClipboard()
    {
        try
        {
            var text = _converter.ShareBase64(_selector.Selected!);
            ImGui.SetClipboardText(text);
        }
        catch (Exception ex)
        {
            Glamourer.Messager.NotificationMessage(ex, $"Could not copy {_selector.Selected!.Name} data to clipboard.",
                $"Could not copy data from design {_selector.Selected!.Identifier} to clipboard", NotificationType.Error, false);
        }
    }

    private void DrawApplyToSelf()
    {
        var (id, data) = _objects.PlayerData;
        if (!ImGuiUtil.DrawDisabledButton("Apply to Yourself", Vector2.Zero,
                "Apply the current design with its settings to your character.\nHold Control to only apply gear.\nHold Shift to only apply customizations.",
                !data.Valid))
            return;

        if (_state.GetOrCreate(id, data.Objects[0], out var state))
        {
            var (applyGear, applyCustomize) = UiHelpers.ConvertKeysToFlags();
            using var _ = _selector.Selected!.TemporarilyRestrictApplication(applyGear, applyCustomize);
            _state.ApplyDesign(_selector.Selected!, state, StateChanged.Source.Manual);
        }
    }

    private void DrawApplyToTarget()
    {
        var (id, data) = _objects.TargetData;
        var tt = id.IsValid
            ? data.Valid
                ? "Apply the current design with its settings to your current target.\nHold Control to only apply gear.\nHold Shift to only apply customizations."
                : "The current target can not be manipulated."
            : "No valid target selected.";
        if (!ImGuiUtil.DrawDisabledButton("Apply to Target", Vector2.Zero, tt, !data.Valid))
            return;

        if (_state.GetOrCreate(id, data.Objects[0], out var state))
        {
            var (applyGear, applyCustomize) = UiHelpers.ConvertKeysToFlags();
            using var _ = _selector.Selected!.TemporarilyRestrictApplication(applyGear, applyCustomize);
            _state.ApplyDesign(_selector.Selected!, state, StateChanged.Source.Manual);
        }
    }

    private void DrawSaveToDat()
    {
        var verified = _importService.Verify(_selector.Selected!.DesignData.Customize, out _);
        var tt = verified
            ? "Export the currently configured customizations of this design to a character creation data file."
            : "The current design contains customizations that can not be applied during character creation.";
        var startPath = GetUserPath();
        if (startPath.Length == 0)
            startPath = null;
        if (ImGuiUtil.DrawDisabledButton("Export to Dat", Vector2.Zero, tt, !verified))
            _fileDialog.SaveFileDialog("Save File...", ".dat", "FFXIV_CHARA_01.dat", ".dat", (v, path) =>
            {
                if (v && _selector.Selected != null)
                    _importService.SaveDesignAsDat(path, _selector.Selected!.DesignData.Customize, _selector.Selected!.Name);
            }, startPath);

        _fileDialog.Draw();
    }

    private void ApplyChanges(ActorState.MetaIndex index, DataChange change, bool value, bool apply)
    {
        switch (change)
        {
            case DataChange.Item:
                _manager.ChangeMeta(_selector.Selected!, index, value);
                break;
            case DataChange.ApplyItem:
                _manager.ChangeApplyMeta(_selector.Selected!, index, apply);
                break;
            case DataChange.Item | DataChange.ApplyItem:
                _manager.ChangeApplyMeta(_selector.Selected!, index, apply);
                _manager.ChangeMeta(_selector.Selected!, index, value);
                break;
        }
    }

    private static unsafe string GetUserPath()
        => Framework.Instance()->UserPath;
}
