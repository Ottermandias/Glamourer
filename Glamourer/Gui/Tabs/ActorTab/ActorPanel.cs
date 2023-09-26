using System;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using Dalamud.Interface.Internal.Notifications;
using FFXIVClientStructs.FFXIV.Client.Game;
using Glamourer.Automation;
using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Gui.Customization;
using Glamourer.Gui.Equipment;
using Glamourer.Interop;
using Glamourer.Interop.Structs;
using Glamourer.Services;
using Glamourer.State;
using Glamourer.Structs;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;

namespace Glamourer.Gui.Tabs.ActorTab;

public class ActorPanel
{
    private readonly ActorSelector       _selector;
    private readonly StateManager        _stateManager;
    private readonly CustomizationDrawer _customizationDrawer;
    private readonly EquipmentDrawer     _equipmentDrawer;
    private readonly IdentifierService   _identification;
    private readonly AutoDesignApplier   _autoDesignApplier;
    private readonly Configuration       _config;
    private readonly DesignConverter     _converter;
    private readonly ObjectManager       _objects;
    private readonly DesignManager       _designManager;
    private readonly DatFileService      _datFileService;
    private readonly Condition           _conditions;

    private ActorIdentifier _identifier;
    private string          _actorName = string.Empty;
    private Actor           _actor     = Actor.Null;
    private ActorData       _data;
    private ActorState?     _state;
    private bool            _lockedRedraw;

    public ActorPanel(ActorSelector selector, StateManager stateManager, CustomizationDrawer customizationDrawer,
        EquipmentDrawer equipmentDrawer, IdentifierService identification, AutoDesignApplier autoDesignApplier,
        Configuration config, DesignConverter converter, ObjectManager objects, DesignManager designManager, DatFileService datFileService,
        Condition conditions)
    {
        _selector            = selector;
        _stateManager        = stateManager;
        _customizationDrawer = customizationDrawer;
        _equipmentDrawer     = equipmentDrawer;
        _identification      = identification;
        _autoDesignApplier   = autoDesignApplier;
        _config              = config;
        _converter           = converter;
        _objects             = objects;
        _designManager       = designManager;
        _datFileService      = datFileService;
        _conditions          = conditions;
    }

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

        if (_datFileService.CreateImGuiTarget(out var dat))
            _stateManager.ChangeCustomize(_state!, dat.Customize, CustomizeApplicationFlags, StateChanged.Source.Manual);
        _datFileService.CreateSource();
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

    private unsafe void DrawPanel()
    {
        using var child = ImRaii.Child("##Panel", -Vector2.One, true);
        if (!child || !_selector.HasSelection || !_stateManager.GetOrCreate(_identifier, _actor, out _state))
            return;

        DrawApplyToSelf();
        ImGui.SameLine();
        DrawApplyToTarget();

        RevertButtons();

        if (_state.ModelData.IsHuman)
            DrawHumanPanel();
        else
            DrawMonsterPanel();
    }

    private void DrawHumanPanel()
    {
        DrawCustomizationsHeader();
        DrawEquipmentHeader();
    }

    private void DrawCustomizationsHeader()
    {
        var header = _state!.ModelData.ModelId == 0
            ? "Customization"
            : $"Customization (Model Id #{_state.ModelData.ModelId})###Customization";
        if (!ImGui.CollapsingHeader(header))
            return;

        if (_customizationDrawer.Draw(_state!.ModelData.Customize, _state.IsLocked, _lockedRedraw))
            _stateManager.ChangeCustomize(_state, _customizationDrawer.Customize, _customizationDrawer.Changed, StateChanged.Source.Manual);

        if (_customizationDrawer.DrawWetnessState(_state!.ModelData.IsWet(), out var newWetness, _state.IsLocked))
            _stateManager.ChangeWetness(_state, newWetness, StateChanged.Source.Manual);
        ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight() / 2));
    }

    private void DrawEquipmentHeader()
    {
        if (!ImGui.CollapsingHeader("Equipment"))
            return;

        _equipmentDrawer.Prepare();

        var usedAllStain = _equipmentDrawer.DrawAllStain(out var newAllStain, _state!.IsLocked);
        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var changes = _equipmentDrawer.DrawEquip(slot, _state!.ModelData, out var newArmor, out var newStain, null, out _, out _,
                _state.IsLocked);
            if (usedAllStain)
            {
                changes  |= DataChange.Stain;
                newStain =  newAllStain;
            }

            switch (changes)
            {
                case DataChange.Item:
                    _stateManager.ChangeItem(_state, slot, newArmor, StateChanged.Source.Manual);
                    break;
                case DataChange.Stain:
                    _stateManager.ChangeStain(_state, slot, newStain, StateChanged.Source.Manual);
                    break;
                case DataChange.Item | DataChange.Stain:
                    _stateManager.ChangeEquip(_state, slot, newArmor, newStain, StateChanged.Source.Manual);
                    break;
            }
        }

        var weaponChanges = _equipmentDrawer.DrawWeapons(_state!.ModelData, out var newMainhand, out var newOffhand, out var newMainhandStain,
            out var newOffhandStain, null, GameMain.IsInGPose(), out _, out _, out _, out _, _state.IsLocked);
        if (usedAllStain)
        {
            weaponChanges    |= DataChange.Stain | DataChange.Stain2;
            newMainhandStain =  newAllStain;
            newOffhandStain  =  newAllStain;
        }

        if (weaponChanges.HasFlag(DataChange.Item))
            if (weaponChanges.HasFlag(DataChange.Stain))
                _stateManager.ChangeEquip(_state, EquipSlot.MainHand, newMainhand, newMainhandStain, StateChanged.Source.Manual);
            else
                _stateManager.ChangeItem(_state, EquipSlot.MainHand, newMainhand, StateChanged.Source.Manual);
        else if (weaponChanges.HasFlag(DataChange.Stain))
            _stateManager.ChangeStain(_state, EquipSlot.MainHand, newMainhandStain, StateChanged.Source.Manual);

        if (weaponChanges.HasFlag(DataChange.Item2))
            if (weaponChanges.HasFlag(DataChange.Stain2))
                _stateManager.ChangeEquip(_state, EquipSlot.OffHand, newOffhand, newOffhandStain, StateChanged.Source.Manual);
            else
                _stateManager.ChangeItem(_state, EquipSlot.OffHand, newOffhand, StateChanged.Source.Manual);
        else if (weaponChanges.HasFlag(DataChange.Stain2))
            _stateManager.ChangeStain(_state, EquipSlot.OffHand, newOffhandStain, StateChanged.Source.Manual);

        ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight() / 2));
        if (EquipmentDrawer.DrawHatState(_state!.ModelData.IsHatVisible(), out var newHatState, _state!.IsLocked))
            _stateManager.ChangeHatState(_state, newHatState, StateChanged.Source.Manual);
        ImGui.SameLine();
        if (EquipmentDrawer.DrawVisorState(_state!.ModelData.IsVisorToggled(), out var newVisorState, _state!.IsLocked))
            _stateManager.ChangeVisorState(_state, newVisorState, StateChanged.Source.Manual);
        ImGui.SameLine();
        if (EquipmentDrawer.DrawWeaponState(_state!.ModelData.IsWeaponVisible(), out var newWeaponState, _state!.IsLocked))
            _stateManager.ChangeWeaponState(_state, newWeaponState, StateChanged.Source.Manual);
        ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight() / 2));
    }

    private void DrawMonsterPanel()
    {
        var names     = _identification.AwaitedService.ModelCharaNames(_state!.ModelData.ModelId);
        var turnHuman = ImGui.Button("Turn Human");
        ImGui.Separator();
        using (var box = ImRaii.ListBox("##MonsterList",
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
        using (var font = ImRaii.PushFont(UiBuilder.MonoFont))
        {
            foreach (var b in _state.ModelData.Customize.Data)
            {
                using (var g = ImRaii.Group())
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

        ImGui.Separator();
        ImGui.TextUnformatted("Equipment Data");
        using (var font = ImRaii.PushFont(UiBuilder.MonoFont))
        {
            foreach (var b in _state.ModelData.GetEquipmentBytes())
            {
                using (var g = ImRaii.Group())
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
            _stateManager.TurnHuman(_state, StateChanged.Source.Manual);
    }

    private HeaderDrawer.Button SetFromClipboardButton()
        => new()
        {
            Description = "Try to apply a design from your clipboard.",
            Icon        = FontAwesomeIcon.Clipboard,
            OnClick     = SetFromClipboard,
            Visible     = _state != null,
            Disabled    = _state?.IsLocked ?? true,
        };

    private HeaderDrawer.Button ExportToClipboardButton()
        => new()
        {
            Description = "Copy the current design to your clipboard.",
            Icon        = FontAwesomeIcon.Copy,
            OnClick     = ExportToClipboard,
            Visible     = _state?.ModelData.ModelId == 0,
        };

    private HeaderDrawer.Button SaveAsDesignButton()
        => new()
        {
            Description = "Save the current state as a design.",
            Icon        = FontAwesomeIcon.Save,
            OnClick     = SaveDesignOpen,
            Visible     = _state?.ModelData.ModelId == 0,
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

    private string      _newName   = string.Empty;
    private DesignBase? _newDesign = null;

    private void SaveDesignOpen()
    {
        ImGui.OpenPopup("Save as Design");
        _newName   = _state!.Identifier.ToName();
        _newDesign = _converter.Convert(_state, EquipFlagExtensions.All, CustomizeFlagExtensions.All);
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
            var text   = ImGui.GetClipboardText();
            var design = _converter.FromBase64(text, true, true, out _) ?? throw new Exception("The clipboard did not contain valid data.");
            _stateManager.ApplyDesign(design, _state!, StateChanged.Source.Manual);
        }
        catch (Exception ex)
        {
            Glamourer.Chat.NotificationMessage(ex, $"Could not apply clipboard to {_identifier}.",
                $"Could not apply clipboard to design {_identifier.Incognito(null)}", "Failure", NotificationType.Error);
        }
    }

    private void ExportToClipboard()
    {
        try
        {
            var text = _converter.ShareBase64(_state!);
            ImGui.SetClipboardText(text);
        }
        catch (Exception ex)
        {
            Glamourer.Chat.NotificationMessage(ex, $"Could not copy {_identifier} data to clipboard.",
                $"Could not copy data from design {_identifier.Incognito(null)} to clipboard", "Failure", NotificationType.Error);
        }
    }

    private void RevertButtons()
    {
        if (ImGuiUtil.DrawDisabledButton("Revert to Game", Vector2.Zero, "Revert the character to its actual state in the game.",
                _state!.IsLocked))
            _stateManager.ResetState(_state!, StateChanged.Source.Manual);

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
        if (!ImGuiUtil.DrawDisabledButton("Apply to Yourself", Vector2.Zero, "Apply the current state to your own character.",
                !data.Valid || id == _identifier || _state!.ModelData.ModelId != 0))
            return;

        if (_stateManager.GetOrCreate(id, data.Objects[0], out var state))
            _stateManager.ApplyDesign(_converter.Convert(_state!, EquipFlagExtensions.All, CustomizeFlagExtensions.AllRelevant), state,
                StateChanged.Source.Manual);
    }

    private void DrawApplyToTarget()
    {
        var (id, data) = _objects.TargetData;
        var tt = id.IsValid
            ? data.Valid
                ? "Apply the current state to your current target."
                : "The current target can not be manipulated."
            : "No valid target selected.";
        if (!ImGuiUtil.DrawDisabledButton("Apply to Target", Vector2.Zero, tt,
                !data.Valid || id == _identifier || _state!.ModelData.ModelId != 0 || _objects.IsInGPose))
            return;

        if (_stateManager.GetOrCreate(id, data.Objects[0], out var state))
            _stateManager.ApplyDesign(_converter.Convert(_state!, EquipFlagExtensions.All, CustomizeFlagExtensions.AllRelevant), state,
                StateChanged.Source.Manual);
    }
}
