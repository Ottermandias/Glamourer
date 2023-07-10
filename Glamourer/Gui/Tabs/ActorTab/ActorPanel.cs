using System;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Internal.Notifications;
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
using Penumbra.GameData.Data;
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

    private ActorIdentifier _identifier;
    private string          _actorName = string.Empty;
    private Actor           _actor     = Actor.Null;
    private ActorData       _data;
    private ActorState?     _state;

    public ActorPanel(ActorSelector selector, StateManager stateManager, CustomizationDrawer customizationDrawer,
        EquipmentDrawer equipmentDrawer, IdentifierService identification, AutoDesignApplier autoDesignApplier,
        Configuration config, DesignConverter converter, ObjectManager objects)
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
    }

    public void Draw()
    {
        using var group = ImRaii.Group();
        (_identifier, _data) = _selector.Selection;
        (_actorName, _actor) = GetHeaderName();
        DrawHeader();
        DrawPanel();
    }

    private void DrawHeader()
    {
        var frameHeight = ImGui.GetFrameHeightWithSpacing();
        var color = !_identifier.IsValid ? ImGui.GetColorU32(ImGuiCol.Text) :
            _data.Valid                  ? ColorId.ActorAvailable.Value() : ColorId.ActorUnavailable.Value();
        var buttonColor = ImGui.GetColorU32(ImGuiCol.FrameBg);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
            .Push(ImGuiStyleVar.FrameRounding, 0);
        ImGuiUtil.DrawTextButton($"{_actorName}##playerHeader", new Vector2(-frameHeight, ImGui.GetFrameHeight()), buttonColor, color);
        ImGui.SameLine();
        style.Push(ImGuiStyleVar.FrameBorderSize, ImGuiHelpers.GlobalScale);
        using (var c = ImRaii.PushColor(ImGuiCol.Text, ColorId.HeaderButtons.Value())
                   .Push(ImGuiCol.Border, ColorId.HeaderButtons.Value()))
        {
            if (ImGuiUtil.DrawDisabledButton(
                    $"{(_selector.IncognitoMode ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash).ToIconString()}###IncognitoMode",
                    new Vector2(frameHeight, ImGui.GetFrameHeight()), string.Empty, false, true))
                _selector.IncognitoMode = !_selector.IncognitoMode;
        }

        var hovered = ImGui.IsItemHovered();
        if (hovered)
            ImGui.SetTooltip(_selector.IncognitoMode ? "Toggle incognito mode off." : "Toggle incognito mode on.");
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

        ApplyClipboardButton();
        ImGui.SameLine();
        CopyToClipboardButton();
        ImGui.SameLine();
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
        if (_customizationDrawer.Draw(_state!.ModelData.Customize, false))
            _stateManager.ChangeCustomize(_state, _customizationDrawer.Customize, _customizationDrawer.Changed, StateChanged.Source.Manual);

        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var stain = _state.ModelData.Stain(slot);
            if (_equipmentDrawer.DrawStain(stain, slot, out var newStain))
                _stateManager.ChangeStain(_state, slot, newStain, StateChanged.Source.Manual);

            ImGui.SameLine();
            var armor = _state.ModelData.Item(slot);
            if (_equipmentDrawer.DrawArmor(armor, slot, out var newArmor, _state.ModelData.Customize.Gender, _state.ModelData.Customize.Race))
                _stateManager.ChangeEquip(_state, slot, newArmor, newStain, StateChanged.Source.Manual);
        }

        var mhStain = _state.ModelData.Stain(EquipSlot.MainHand);
        if (_equipmentDrawer.DrawStain(mhStain, EquipSlot.MainHand, out var newMhStain))
            _stateManager.ChangeStain(_state, EquipSlot.MainHand, newMhStain, StateChanged.Source.Manual);

        ImGui.SameLine();
        var mh = _state.ModelData.Item(EquipSlot.MainHand);
        if (_equipmentDrawer.DrawMainhand(mh, false, out var newMh))
            _stateManager.ChangeEquip(_state, EquipSlot.MainHand, newMh, newMhStain, StateChanged.Source.Manual);

        if (newMh.Type.Offhand() is not FullEquipType.Unknown)
        {
            var ohStain = _state.ModelData.Stain(EquipSlot.OffHand);
            if (_equipmentDrawer.DrawStain(ohStain, EquipSlot.OffHand, out var newOhStain))
                _stateManager.ChangeStain(_state, EquipSlot.OffHand, newOhStain, StateChanged.Source.Manual);

            ImGui.SameLine();
            var oh = _state.ModelData.Item(EquipSlot.OffHand);
            if (_equipmentDrawer.DrawMainhand(oh, false, out var newOh))
                _stateManager.ChangeEquip(_state, EquipSlot.OffHand, newOh, newOhStain, StateChanged.Source.Manual);
        }
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

    private void ApplyClipboardButton()
    {
        if (!ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Clipboard.ToIconString(), new Vector2(ImGui.GetFrameHeight()),
                "Try to apply a design from your clipboard.", false, true))
            return;

        try
        {
            var text   = ImGui.GetClipboardText();
            var design = _converter.FromBase64(text, true, true) ?? throw new Exception("The clipboard did not contain valid data.");
            _stateManager.ApplyDesign(design, _state!, StateChanged.Source.Manual);
        }
        catch (Exception ex)
        {
            Glamourer.Chat.NotificationMessage(ex, $"Could not apply clipboard to {_identifier}.",
                $"Could not apply clipboard to design {_identifier.Incognito(null)}", "Failure", NotificationType.Error);
        }
    }

    private void CopyToClipboardButton()
    {
        if (!ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Copy.ToIconString(), new Vector2(ImGui.GetFrameHeight()),
                "Copy the current design to your clipboard.", false, true))
            return;

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
        if (ImGui.Button("Revert to Game"))
            _stateManager.ResetState(_state!);

        ImGui.SameLine();
        if (ImGui.Button("Reapply State"))
            _stateManager.ReapplyState(_actor);

        ImGui.SameLine();
        if (ImGuiUtil.DrawDisabledButton("Reapply Automation", Vector2.Zero, string.Empty, !_config.EnableAutoDesigns))
        {
            _autoDesignApplier.ReapplyAutomation(_actor, _identifier, _state!);
            _stateManager.ReapplyState(_actor);
        }
    }

    private void DrawApplyToSelf()
    {
        var (id, data) = _objects.PlayerData;
        if (!ImGuiUtil.DrawDisabledButton("Apply to Yourself", Vector2.Zero, "Apply the current state to your own character.",
                !data.Valid || id == _identifier))
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
        if (!ImGuiUtil.DrawDisabledButton("Apply to Target", Vector2.Zero, tt, !data.Valid || id == _identifier))
            return;

        if (_stateManager.GetOrCreate(id, data.Objects[0], out var state))
            _stateManager.ApplyDesign(_converter.Convert(_state!, EquipFlagExtensions.All, CustomizeFlagExtensions.AllRelevant), state,
                StateChanged.Source.Manual);
    }
}
