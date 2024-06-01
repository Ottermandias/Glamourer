﻿using Dalamud.Game.ClientState.Conditions;
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
using Glamourer.Interop.Structs;
using Glamourer.State;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;
using Penumbra.GameData.Actors;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;
using ObjectManager = Glamourer.Interop.ObjectManager;

namespace Glamourer.Gui.Tabs.ActorTab;

public class ActorPanel
{
    private readonly ActorSelector            _selector;
    private readonly StateManager             _stateManager;
    private readonly CustomizationDrawer      _customizationDrawer;
    private readonly EquipmentDrawer          _equipmentDrawer;
    private readonly AutoDesignApplier        _autoDesignApplier;
    private readonly Configuration            _config;
    private readonly DesignConverter          _converter;
    private readonly ObjectManager            _objects;
    private readonly DesignManager            _designManager;
    private readonly ImportService            _importService;
    private readonly ICondition               _conditions;
    private readonly DictModelChara           _modelChara;
    private readonly CustomizeParameterDrawer _parameterDrawer;
    private readonly AdvancedDyePopup         _advancedDyes;
    private readonly HeaderDrawer.Button[]   _leftButtons;
    private readonly HeaderDrawer.Button[]   _rightButtons;

    public ActorPanel(ActorSelector selector,
        StateManager stateManager,
        CustomizationDrawer customizationDrawer,
        EquipmentDrawer equipmentDrawer,
        AutoDesignApplier autoDesignApplier,
        Configuration config,
        DesignConverter converter,
        ObjectManager objects,
        DesignManager designManager,
        ImportService importService,
        ICondition conditions,
        DictModelChara modelChara,
        CustomizeParameterDrawer parameterDrawer,
        AdvancedDyePopup advancedDyes)
    {
        _selector            = selector;
        _stateManager        = stateManager;
        _customizationDrawer = customizationDrawer;
        _equipmentDrawer     = equipmentDrawer;
        _autoDesignApplier   = autoDesignApplier;
        _config              = config;
        _converter           = converter;
        _objects             = objects;
        _designManager       = designManager;
        _importService       = importService;
        _conditions          = conditions;
        _modelChara          = modelChara;
        _parameterDrawer     = parameterDrawer;
        _advancedDyes        = advancedDyes;
        _leftButtons =
        [
            new SetFromClipboardButton(this),
            new ExportToClipboardButton(this),
            new SaveAsDesignButton(this),
        ];
        _rightButtons =
        [
            new LockedButton(this),
            new HeaderDrawer.IncognitoButton(_config.Ephemeral),
        ];
    }


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
        HeaderDrawer.Draw(_actorName, textColor, ImGui.GetColorU32(ImGuiCol.FrameBg), _leftButtons, _rightButtons);

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

        using var disabled = ImRaii.Disabled(transformationId != 0);
        if (_state.ModelData.IsHuman)
            DrawHumanPanel();
        else
            DrawMonsterPanel();
        _advancedDyes.Draw(_actor, _state);
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

    private string      _newName = string.Empty;
    private DesignBase? _newDesign;

    private void SaveDesignDrawPopup()
    {
        if (!ImGuiUtil.OpenNameField("Save as Design", ref _newName))
            return;

        if (_newDesign != null && _newName.Length > 0)
            _designManager.CreateClone(_newDesign, _newName, true);
        _newDesign = null;
        _newName   = string.Empty;
    }

    private void RevertButtons()
    {
        if (ImGuiUtil.DrawDisabledButton("Revert to Game", Vector2.Zero, "Revert the character to its actual state in the game.",
                _state!.IsLocked))
            _stateManager.ResetState(_state!, StateSource.Manual);

        ImGui.SameLine();

        if (ImGuiUtil.DrawDisabledButton("Reapply Automation", Vector2.Zero,
                "Reapply the current automation state for the character on top of its current state..",
                !_config.EnableAutoDesigns || _state!.IsLocked))
        {
            _autoDesignApplier.ReapplyAutomation(_actor, _identifier, _state!, false, out var forcedRedraw);
            _stateManager.ReapplyState(_actor, forcedRedraw, StateSource.Manual);
        }

        ImGui.SameLine();
        if (ImGuiUtil.DrawDisabledButton("Revert to Automation", Vector2.Zero,
                "Try to revert the character to the state it would have using automated designs.",
                !_config.EnableAutoDesigns || _state!.IsLocked))
        {
            _autoDesignApplier.ReapplyAutomation(_actor, _identifier, _state!, true, out var forcedRedraw);
            _stateManager.ReapplyState(_actor, forcedRedraw, StateSource.Manual);
        }

        ImGui.SameLine();
        if (ImGuiUtil.DrawDisabledButton("Reapply", Vector2.Zero,
                "Try to reapply the configured state if something went wrong. Should generally not be necessary.",
                _state!.IsLocked))
            _stateManager.ReapplyState(_actor, false, StateSource.Manual);
    }

    private void DrawApplyToSelf()
    {
        var (id, data) = _objects.PlayerData;
        if (!ImGuiUtil.DrawDisabledButton("Apply to Yourself", Vector2.Zero,
                "Apply the current state to your own character.\nHold Control to only apply gear.\nHold Shift to only apply customizations.",
                !data.Valid || id == _identifier || _state!.ModelData.ModelId != 0))
            return;

        if (_stateManager.GetOrCreate(id, data.Objects[0], out var state))
            _stateManager.ApplyDesign(state, _converter.Convert(_state!, ApplicationRules.FromModifiers(_state!)),
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

        if (_stateManager.GetOrCreate(id, data.Objects[0], out var state))
            _stateManager.ApplyDesign(state, _converter.Convert(_state!, ApplicationRules.FromModifiers(_state!)),
                ApplySettings.Manual);
    }


    private sealed class SetFromClipboardButton(ActorPanel panel)
        : HeaderDrawer.Button
    {
        protected override string Description
            => "Try to apply a design from your clipboard.\nHold Control to only apply gear.\nHold Shift to only apply customizations.";

        protected override FontAwesomeIcon Icon
            => FontAwesomeIcon.Clipboard;

        public override bool Visible
            => panel._state != null;

        protected override bool Disabled
            => panel._state?.IsLocked ?? true;

        protected override void OnClick()
        {
            try
            {
                var (applyGear, applyCustomize) = UiHelpers.ConvertKeysToBool();
                var text = ImGui.GetClipboardText();
                var design = panel._converter.FromBase64(text, applyCustomize, applyGear, out _)
                 ?? throw new Exception("The clipboard did not contain valid data.");
                panel._stateManager.ApplyDesign(panel._state!, design, ApplySettings.ManualWithLinks);
            }
            catch (Exception ex)
            {
                Glamourer.Messager.NotificationMessage(ex, $"Could not apply clipboard to {panel._identifier}.",
                    $"Could not apply clipboard to design {panel._identifier.Incognito(null)}", NotificationType.Error, false);
            }
        }
    }

    private sealed class ExportToClipboardButton(ActorPanel panel) : HeaderDrawer.Button
    {
        protected override string Description
            => "Copy the current design to your clipboard.\nHold Control to disable applying of customizations for the copied design.\nHold Shift to disable applying of gear for the copied design.";

        protected override FontAwesomeIcon Icon
            => FontAwesomeIcon.Copy;

        public override bool Visible
            => panel._state?.ModelData.ModelId == 0;

        protected override void OnClick()
        {
            try
            {
                var text = panel._converter.ShareBase64(panel._state!, ApplicationRules.FromModifiers(panel._state!));
                ImGui.SetClipboardText(text);
            }
            catch (Exception ex)
            {
                Glamourer.Messager.NotificationMessage(ex, $"Could not copy {panel._identifier} data to clipboard.",
                    $"Could not copy data from design {panel._identifier.Incognito(null)} to clipboard", NotificationType.Error);
            }
        }
    }

    private sealed class SaveAsDesignButton(ActorPanel panel) : HeaderDrawer.Button
    {
        protected override string Description
            => "Save the current state as a design.\nHold Control to disable applying of customizations for the saved design.\nHold Shift to disable applying of gear for the saved design.";

        protected override FontAwesomeIcon Icon
            => FontAwesomeIcon.Save;

        public override bool Visible
            => panel._state?.ModelData.ModelId == 0;

        protected override void OnClick()
        {
            ImGui.OpenPopup("Save as Design");
            panel._newName   = panel._state!.Identifier.ToName();
            panel._newDesign = panel._converter.Convert(panel._state, ApplicationRules.FromModifiers(panel._state));
        }
    }

    private sealed class LockedButton(ActorPanel panel) : HeaderDrawer.Button
    {
        protected override string Description
            => "The current state of this actor is locked by external tools.";

        protected override FontAwesomeIcon Icon
            => FontAwesomeIcon.Lock;

        public override bool Visible
            => panel._state?.IsLocked ?? false;

        protected override bool Disabled
            => true;

        protected override uint BorderColor
            => ColorId.ActorUnavailable.Value();

        protected override uint TextColor
            => ColorId.ActorUnavailable.Value();

        protected override void OnClick()
        { }
    }
}
