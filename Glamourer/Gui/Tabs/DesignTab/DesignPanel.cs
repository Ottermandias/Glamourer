using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
            var data = EquipDrawData.FromDesign(_manager, _selector.Selected!, slot);
            _equipmentDrawer.DrawEquip(data);
            if (usedAllStain)
                _manager.ChangeStain(_selector.Selected, slot, newAllStain);
        }

        var mainhand = EquipDrawData.FromDesign(_manager, _selector.Selected!, EquipSlot.MainHand);
        var offhand  = EquipDrawData.FromDesign(_manager, _selector.Selected!, EquipSlot.OffHand);
        _equipmentDrawer.DrawWeapons(mainhand, offhand, true);
        ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight() / 2));
        DrawEquipmentMetaToggles();
        ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight() / 2));
    }

    private void DrawEquipmentMetaToggles()
    {
        EquipmentDrawer.DrawMetaToggle(ToggleDrawData.FromDesign(ActorState.MetaIndex.HatState, _manager, _selector.Selected!));
        ImGui.SameLine();
        EquipmentDrawer.DrawMetaToggle(ToggleDrawData.FromDesign(ActorState.MetaIndex.VisorState, _manager, _selector.Selected!));
        ImGui.SameLine();
        EquipmentDrawer.DrawMetaToggle(ToggleDrawData.FromDesign(ActorState.MetaIndex.WeaponState, _manager, _selector.Selected!));
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

        EquipmentDrawer.DrawMetaToggle(ToggleDrawData.FromDesign(ActorState.MetaIndex.Wetness, _manager, _selector.Selected!));
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
            void ApplyEquip(string label, EquipFlag allFlags, bool stain, IEnumerable<EquipSlot> slots)
            {
                var flags = (uint)(allFlags & _selector.Selected!.ApplyEquip);

                var bigChange = ImGui.CheckboxFlags($"Apply All {label}", ref flags, (uint)allFlags);
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
                EquipSlotExtensions.FullSlots);

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
                Glamourer.Messager.NotificationMessage(
                    $"Applied games .dat file {dat.Description} customizations to {_selector.Selected.Name}.", NotificationType.Success, false);
            }
            else if (_importService.CreateCharaTarget(out var designBase, out var name))
            {
                _manager.ApplyDesign(_selector.Selected!, designBase);
                Glamourer.Messager.NotificationMessage($"Applied Anamnesis .chara file {name} to {_selector.Selected.Name}.",
                    NotificationType.Success, false);
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

    private static unsafe string GetUserPath()
        => Framework.Instance()->UserPath;
}
