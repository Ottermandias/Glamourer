using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Glamourer.Automation;
using Glamourer.Designs;
using Glamourer.Gui.Customization;
using Glamourer.Gui.Equipment;
using Glamourer.Gui.Materials;
using Glamourer.Interop;
using Glamourer.Interop.Material;
using Glamourer.Interop.Structs;
using Glamourer.State;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;
using Penumbra.GameData.Actors;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;

namespace Glamourer.Gui.Tabs.ActorTab;

public class ActorPanel(
    ActorSelector _selector,
    StateManager _stateManager,
    CustomizationDrawer _customizationDrawer,
    EquipmentDrawer _equipmentDrawer,
    AutoDesignApplier _autoDesignApplier,
    Configuration _config,
    DesignConverter _converter,
    ObjectManager _objects,
    DesignManager _designManager,
    ImportService _importService,
    ICondition _conditions,
    DictModelChara _modelChara,
    CustomizeParameterDrawer _parameterDrawer,
    MaterialDrawer _materialDrawer)
{
    private ActorIdentifier _identifier;
    private string          _actorName = string.Empty;
    private Actor           _actor     = Actor.Null;
    private ActorData       _data;
    private ActorState?     _state;
    private bool            _lockedRedraw;

    private CustomizeFlag CustomizeApplicationFlags
        => _lockedRedraw ? CustomizeFlagExtensions.AllRelevant & ~CustomizeFlagExtensions.RedrawRequired : CustomizeFlagExtensions.AllRelevant;

    public void Draw()
    {
        using var group = ImRaii.Group();
        (_identifier, _data) = _selector.Selection;
        _lockedRedraw = _identifier.Type is IdentifierType.Special
         || _conditions[ConditionFlag.OccupiedInCutSceneEvent];
        (_actorName, _actor) = GetHeaderName();
        DrawHeader();
        DrawPanel();

        if (_state is not { IsLocked: false })
            return;

        if (_importService.CreateDatTarget(out var dat))
        {
            _stateManager.ChangeEntireCustomize(_state!, dat.Customize, CustomizeApplicationFlags, ApplySettings.Manual);
            Glamourer.Messager.NotificationMessage($"Applied games .dat file {dat.Description} customizations to {_state.Identifier}.",
                NotificationType.Success, false);
        }
        else if (_importService.CreateCharaTarget(out var designBase, out var name))
        {
            _stateManager.ApplyDesign(_state!, designBase, ApplySettings.Manual);
            Glamourer.Messager.NotificationMessage($"Applied Anamnesis .chara file {name} to {_state.Identifier}.", NotificationType.Success,
                false);
        }

        _importService.CreateDatSource();
        _importService.CreateCharaSource();
    }

    private void DrawHeader()
    {
        var textColor = !_identifier.IsValid ? ImGui.GetColorU32(ImGuiCol.Text) :
            _data.Valid                      ? ColorId.ActorAvailable.Value() : ColorId.ActorUnavailable.Value();
        HeaderDrawer.Draw(_actorName, textColor, ImGui.GetColorU32(ImGuiCol.FrameBg),
            3, SetFromClipboardButton(), ExportToClipboardButton(), SaveAsDesignButton(), LockedButton(),
            HeaderDrawer.Button.IncognitoButton(_selector.IncognitoMode, v => _selector.IncognitoMode = v));

        SaveDesignDrawPopup();
    }

    private (string, Actor) GetHeaderName()
    {
        if (!_identifier.IsValid)
            return ("No Selection", Actor.Null);

        if (_data.Valid)
            return (_selector.IncognitoMode ? _identifier.Incognito(_data.Label) : _data.Label, _data.Objects[0]);

        return (_selector.IncognitoMode ? _identifier.Incognito(null) : _identifier.ToString(), Actor.Null);
    }

    private Vector3                            _test;
    private int                                _rowId;
    private MaterialValueIndex.ColorTableIndex _index;
    private int                                _materialId;
    private int                                _slotId;

    private unsafe void DrawPanel()
    {
        using var child = ImRaii.Child("##Panel", -Vector2.One, true);
        if (!child || !_selector.HasSelection || !_stateManager.GetOrCreate(_identifier, _actor, out _state))
            return;

        var transformationId = _actor.IsCharacter ? _actor.AsCharacter->CharacterData.TransformationId : 0;
        if (transformationId != 0)
            ImGuiUtil.DrawTextButton($"Currently transformed to Transformation {transformationId}.",
                -Vector2.UnitX, Colors.SelectedRed);

        DrawApplyToSelf();
        ImGui.SameLine();
        DrawApplyToTarget();

        RevertButtons();


        if (ImGui.CollapsingHeader("Material Shit"))
            _materialDrawer.DrawPanel(_actor);
        ImGui.InputInt("Row", ref _rowId);
        ImGui.InputInt("Material", ref _materialId);
        ImGui.InputInt("Slot", ref _slotId);
        ImGuiUtil.GenericEnumCombo("Value", 300, _index, out _index);

        var index = new MaterialValueIndex(MaterialValueIndex.DrawObjectType.Human, (byte) _slotId, (byte) _materialId, (byte)_rowId, _index);
        index.TryGetValue(_actor, out var current);
        _test = current;
        if (ImGui.ColorPicker3("TestPicker", ref _test) && _actor.Valid)
            _state.Materials.AddOrUpdateValue(index, new MaterialValueState(current, _test, StateSource.Manual));

        if (ImGui.ColorPicker3("TestPicker2", ref _test) && _actor.Valid)
            _state.Materials.AddOrUpdateValue(index, new MaterialValueState(current, _test, StateSource.Fixed));

        foreach (var value in _state.Materials.Values)
        {
            var id = MaterialValueIndex.FromKey(value.Key);
            ImGui.TextUnformatted($"{id.DrawObject} {id.SlotIndex} {id.MaterialIndex} {id.RowIndex} {id.DataIndex} ");
            ImGui.SameLine(0, 0);
            var game = ImGui.ColorConvertFloat4ToU32(new Vector4(value.Value.Game, 1));
            ImGuiUtil.DrawTextButton("   ", Vector2.Zero, game);
            ImGui.SameLine(0, ImGui.GetStyle().ItemSpacing.X);
            var model = ImGui.ColorConvertFloat4ToU32(new Vector4(value.Value.Model, 1));
            ImGuiUtil.DrawTextButton("   ", Vector2.Zero, model);
            ImGui.SameLine(0, 0);
            ImGui.TextUnformatted($" {value.Value.Source}");
        }

        using var disabled = ImRaii.Disabled(transformationId != 0);
        if (_state.ModelData.IsHuman)
            DrawHumanPanel();
        else
            DrawMonsterPanel();
    }

    private void DrawHumanPanel()
    {
        DrawCustomizationsHeader();
        DrawEquipmentHeader();
        DrawParameterHeader();
    }

    private void DrawCustomizationsHeader()
    {
        var header = _state!.ModelData.ModelId == 0
            ? "Customization"
            : $"Customization (Model Id #{_state.ModelData.ModelId})###Customization";
        using var h = ImRaii.CollapsingHeader(header);
        if (!h)
            return;

        if (_customizationDrawer.Draw(_state!.ModelData.Customize, _state.IsLocked, _lockedRedraw))
            _stateManager.ChangeEntireCustomize(_state, _customizationDrawer.Customize, _customizationDrawer.Changed, ApplySettings.Manual);

        EquipmentDrawer.DrawMetaToggle(ToggleDrawData.FromState(MetaIndex.Wetness, _stateManager, _state));
        ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight() / 2));
    }

    private void DrawEquipmentHeader()
    {
        using var h = ImRaii.CollapsingHeader("Equipment");
        if (!h)
            return;

        _equipmentDrawer.Prepare();

        var usedAllStain = _equipmentDrawer.DrawAllStain(out var newAllStain, _state!.IsLocked);
        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var data = EquipDrawData.FromState(_stateManager, _state!, slot);
            _equipmentDrawer.DrawEquip(data);
            if (usedAllStain)
                _stateManager.ChangeStain(_state, slot, newAllStain, ApplySettings.Manual);
        }

        var mainhand = EquipDrawData.FromState(_stateManager, _state, EquipSlot.MainHand);
        var offhand  = EquipDrawData.FromState(_stateManager, _state, EquipSlot.OffHand);
        _equipmentDrawer.DrawWeapons(mainhand, offhand, GameMain.IsInGPose());

        ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight() / 2));
        DrawEquipmentMetaToggles();
        ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight() / 2));
    }

    private void DrawParameterHeader()
    {
        if (!_config.UseAdvancedParameters)
            return;

        using var h = ImRaii.CollapsingHeader("Advanced Customizations");
        if (!h)
            return;

        _parameterDrawer.Draw(_stateManager, _state!);
    }

    private void DrawEquipmentMetaToggles()
    {
        using (_ = ImRaii.Group())
        {
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.FromState(MetaIndex.HatState, _stateManager, _state!));
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.CrestFromState(CrestFlag.Head, _stateManager, _state!));
        }

        ImGui.SameLine();
        using (_ = ImRaii.Group())
        {
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.FromState(MetaIndex.VisorState, _stateManager, _state!));
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.CrestFromState(CrestFlag.Body, _stateManager, _state!));
        }

        ImGui.SameLine();
        using (_ = ImRaii.Group())
        {
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.FromState(MetaIndex.WeaponState, _stateManager, _state!));
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.CrestFromState(CrestFlag.OffHand, _stateManager, _state!));
        }
    }

    private void DrawMonsterPanel()
    {
        var names     = _modelChara[_state!.ModelData.ModelId];
        var turnHuman = ImGui.Button("Turn Human");
        ImGui.Separator();
        using (_ = ImRaii.ListBox("##MonsterList",
                   new Vector2(ImGui.GetContentRegionAvail().X, 10 * ImGui.GetTextLineHeightWithSpacing())))
        {
            if (names.Count == 0)
                ImGui.TextUnformatted("Unknown Monster");
            else
                ImGuiClip.ClippedDraw(names, p => ImGui.TextUnformatted($"{p.Name} ({p.Kind.ToName()} #{p.Id})"),
                    ImGui.GetTextLineHeightWithSpacing());
        }

        ImGui.Separator();
        ImGui.TextUnformatted("Customization Data");
        using (_ = ImRaii.PushFont(UiBuilder.MonoFont))
        {
            foreach (var b in _state.ModelData.Customize)
            {
                using (_ = ImRaii.Group())
                {
                    ImGui.TextUnformatted($" {b.Value:X2}");
                    ImGui.TextUnformatted($"{b.Value,3}");
                }

                ImGui.SameLine();
                if (ImGui.GetContentRegionAvail().X < ImGui.GetStyle().ItemSpacing.X + ImGui.CalcTextSize("XXX").X)
                    ImGui.NewLine();
            }

            if (ImGui.GetCursorPosX() != 0)
                ImGui.NewLine();
        }

        ImGui.Separator();
        ImGui.TextUnformatted("Equipment Data");
        using (_ = ImRaii.PushFont(UiBuilder.MonoFont))
        {
            foreach (var b in _state.ModelData.GetEquipmentBytes())
            {
                using (_ = ImRaii.Group())
                {
                    ImGui.TextUnformatted($" {b:X2}");
                    ImGui.TextUnformatted($"{b,3}");
                }

                ImGui.SameLine();
                if (ImGui.GetContentRegionAvail().X < ImGui.GetStyle().ItemSpacing.X + ImGui.CalcTextSize("XXX").X)
                    ImGui.NewLine();
            }

            if (ImGui.GetCursorPosX() != 0)
                ImGui.NewLine();
        }

        if (turnHuman)
            _stateManager.TurnHuman(_state, StateSource.Manual);
    }

    private HeaderDrawer.Button SetFromClipboardButton()
        => new()
        {
            Description =
                "Try to apply a design from your clipboard.\nHold Control to only apply gear.\nHold Shift to only apply customizations.",
            Icon     = FontAwesomeIcon.Clipboard,
            OnClick  = SetFromClipboard,
            Visible  = _state != null,
            Disabled = _state?.IsLocked ?? true,
        };

    private HeaderDrawer.Button ExportToClipboardButton()
        => new()
        {
            Description =
                "Copy the current design to your clipboard.\nHold Control to disable applying of customizations for the copied design.\nHold Shift to disable applying of gear for the copied design.",
            Icon    = FontAwesomeIcon.Copy,
            OnClick = ExportToClipboard,
            Visible = _state?.ModelData.ModelId == 0,
        };

    private HeaderDrawer.Button SaveAsDesignButton()
        => new()
        {
            Description =
                "Save the current state as a design.\nHold Control to disable applying of customizations for the saved design.\nHold Shift to disable applying of gear for the saved design.",
            Icon    = FontAwesomeIcon.Save,
            OnClick = SaveDesignOpen,
            Visible = _state?.ModelData.ModelId == 0,
        };

    private HeaderDrawer.Button LockedButton()
        => new()
        {
            Description = "The current state of this actor is locked by external tools.",
            Icon        = FontAwesomeIcon.Lock,
            OnClick     = () => { },
            Disabled    = true,
            Visible     = _state?.IsLocked ?? false,
            TextColor   = ColorId.ActorUnavailable.Value(),
            BorderColor = ColorId.ActorUnavailable.Value(),
        };

    private string      _newName = string.Empty;
    private DesignBase? _newDesign;

    private void SaveDesignOpen()
    {
        ImGui.OpenPopup("Save as Design");
        _newName = _state!.Identifier.ToName();
        var (applyGear, applyCustomize, applyCrest, applyParameters) = UiHelpers.ConvertKeysToFlags();
        _newDesign = _converter.Convert(_state, applyGear, applyCustomize, applyCrest, applyParameters);
    }

    private void SaveDesignDrawPopup()
    {
        if (!ImGuiUtil.OpenNameField("Save as Design", ref _newName))
            return;

        if (_newDesign != null && _newName.Length > 0)
            _designManager.CreateClone(_newDesign, _newName, true);
        _newDesign = null;
        _newName   = string.Empty;
    }

    private void SetFromClipboard()
    {
        try
        {
            var (applyGear, applyCustomize) = UiHelpers.ConvertKeysToBool();
            var text = ImGui.GetClipboardText();
            var design = _converter.FromBase64(text, applyCustomize, applyGear, out _)
             ?? throw new Exception("The clipboard did not contain valid data.");
            _stateManager.ApplyDesign(_state!, design, ApplySettings.Manual with { MergeLinks = true });
        }
        catch (Exception ex)
        {
            Glamourer.Messager.NotificationMessage(ex, $"Could not apply clipboard to {_identifier}.",
                $"Could not apply clipboard to design {_identifier.Incognito(null)}", NotificationType.Error, false);
        }
    }

    private void ExportToClipboard()
    {
        try
        {
            var (applyGear, applyCustomize, applyCrest, applyParameters) = UiHelpers.ConvertKeysToFlags();
            var text = _converter.ShareBase64(_state!, applyGear, applyCustomize, applyCrest, applyParameters);
            ImGui.SetClipboardText(text);
        }
        catch (Exception ex)
        {
            Glamourer.Messager.NotificationMessage(ex, $"Could not copy {_identifier} data to clipboard.",
                $"Could not copy data from design {_identifier.Incognito(null)} to clipboard", NotificationType.Error);
        }
    }

    private void RevertButtons()
    {
        if (ImGuiUtil.DrawDisabledButton("Revert to Game", Vector2.Zero, "Revert the character to its actual state in the game.",
                _state!.IsLocked))
            _stateManager.ResetState(_state!, StateSource.Manual);

        ImGui.SameLine();
        if (ImGuiUtil.DrawDisabledButton("Reapply State", Vector2.Zero, "Try to reapply the configured state if something went wrong.",
                _state!.IsLocked))
            _stateManager.ReapplyState(_actor);

        ImGui.SameLine();
        if (ImGuiUtil.DrawDisabledButton("Reapply Automation", Vector2.Zero,
                "Try to revert the character to the state it would have using automated designs.",
                !_config.EnableAutoDesigns || _state!.IsLocked))
        {
            _autoDesignApplier.ReapplyAutomation(_actor, _identifier, _state!);
            _stateManager.ReapplyState(_actor);
        }
    }

    private void DrawApplyToSelf()
    {
        var (id, data) = _objects.PlayerData;
        if (!ImGuiUtil.DrawDisabledButton("Apply to Yourself", Vector2.Zero,
                "Apply the current state to your own character.\nHold Control to only apply gear.\nHold Shift to only apply customizations.",
                !data.Valid || id == _identifier || _state!.ModelData.ModelId != 0))
            return;

        var (applyGear, applyCustomize, applyCrest, applyParameters) = UiHelpers.ConvertKeysToFlags();
        if (_stateManager.GetOrCreate(id, data.Objects[0], out var state))
            _stateManager.ApplyDesign(state, _converter.Convert(_state!, applyGear, applyCustomize, applyCrest, applyParameters),
                ApplySettings.Manual);
    }

    private void DrawApplyToTarget()
    {
        var (id, data) = _objects.TargetData;
        var tt = id.IsValid
            ? data.Valid
                ? "Apply the current state to your current target.\nHold Control to only apply gear.\nHold Shift to only apply customizations."
                : "The current target can not be manipulated."
            : "No valid target selected.";
        if (!ImGuiUtil.DrawDisabledButton("Apply to Target", Vector2.Zero, tt,
                !data.Valid || id == _identifier || _state!.ModelData.ModelId != 0))
            return;

        var (applyGear, applyCustomize, applyCrest, applyParameters) = UiHelpers.ConvertKeysToFlags();
        if (_stateManager.GetOrCreate(id, data.Objects[0], out var state))
            _stateManager.ApplyDesign(state, _converter.Convert(_state!, applyGear, applyCustomize, applyCrest, applyParameters),
                ApplySettings.Manual);
    }
}
