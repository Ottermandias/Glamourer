using System;
using System.Numerics;
using System.Xml.Linq;
using Dalamud.Interface;
using Glamourer.Events;
using Glamourer.Gui.Customization;
using Glamourer.Gui.Equipment;
using Glamourer.Interop.Structs;
using Glamourer.Services;
using Glamourer.State;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Data;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Tabs.ActorTab;

public class ActorPanel
{
    private readonly ActorSelector       _selector;
    private readonly StateManager        _stateManager;
    private readonly CustomizationDrawer _customizationDrawer;
    private readonly EquipmentDrawer     _equipmentDrawer;
    private readonly HumanModelList      _humans;
    private readonly IdentifierService   _identification;

    private ActorIdentifier _identifier;
    private string          _actorName = string.Empty;
    private Actor           _actor     = Actor.Null;
    private ActorData       _data;
    private ActorState?     _state;

    public ActorPanel(ActorSelector selector, StateManager stateManager, CustomizationDrawer customizationDrawer,
        EquipmentDrawer equipmentDrawer, HumanModelList humans, IdentifierService identification)
    {
        _selector            = selector;
        _stateManager        = stateManager;
        _customizationDrawer = customizationDrawer;
        _equipmentDrawer     = equipmentDrawer;
        _humans              = humans;
        _identification      = identification;
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

    private void DrawHumanPanel()
    {
        if (_customizationDrawer.Draw(_state!.ModelData.Customize, false))
            _stateManager.ChangeCustomize(_state, _customizationDrawer.Customize, _customizationDrawer.Changed, StateChanged.Source.Manual);

        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var stain = _state.ModelData.Stain(slot);
            if (_equipmentDrawer.DrawStain(stain, slot, out var newStain))
                _stateManager.ChangeStain(_state, slot, newStain.RowIndex, StateChanged.Source.Manual);

            ImGui.SameLine();
            var armor = _state.ModelData.Item(slot);
            if (_equipmentDrawer.DrawArmor(armor, slot, out var newArmor, _state.ModelData.Customize.Gender, _state.ModelData.Customize.Race))
                _stateManager.ChangeEquip(_state, slot, newArmor, newStain.RowIndex, StateChanged.Source.Manual);
        }

        var mhStain = _state.ModelData.Stain(EquipSlot.MainHand);
        if (_equipmentDrawer.DrawStain(mhStain, EquipSlot.MainHand, out var newMhStain))
            _stateManager.ChangeStain(_state, EquipSlot.MainHand, newMhStain.RowIndex, StateChanged.Source.Manual);

        ImGui.SameLine();
        var mh = _state.ModelData.Item(EquipSlot.MainHand);
        if (_equipmentDrawer.DrawMainhand(mh, false, out var newMh))
            _stateManager.ChangeEquip(_state, EquipSlot.MainHand, newMh, newMhStain.RowIndex, StateChanged.Source.Manual);

        if (newMh.Type.Offhand() is not FullEquipType.Unknown)
        {
            var ohStain = _state.ModelData.Stain(EquipSlot.OffHand);
            if (_equipmentDrawer.DrawStain(ohStain, EquipSlot.OffHand, out var newOhStain))
                _stateManager.ChangeStain(_state, EquipSlot.OffHand, newOhStain.RowIndex, StateChanged.Source.Manual);

            ImGui.SameLine();
            var oh = _state.ModelData.Item(EquipSlot.OffHand);
            if (_equipmentDrawer.DrawMainhand(oh, false, out var newOh))
                _stateManager.ChangeEquip(_state, EquipSlot.OffHand, newOh, newOhStain.RowIndex, StateChanged.Source.Manual);
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

    private unsafe void DrawPanel()
    {
        using var child = ImRaii.Child("##Panel", -Vector2.One, true);
        if (!child || !_selector.HasSelection || !_stateManager.GetOrCreate(_identifier, _actor, out _state))
            return;

        if (_humans.IsHuman(_state.ModelData.ModelId))
            DrawHumanPanel();
        else
            DrawMonsterPanel();
    }


    private unsafe void RevertButton()
    {
        //if (ImGui.Button("Revert"))
        //    _activeDesigns.RevertDesign(_currentSave!);
        //foreach (var actor in _currentData.Objects)
        //    _currentSave!.ApplyToActor(actor);
        //
        //if (_currentData.Objects.Count > 0)
        //    _currentSave = _manipulations.GetOrCreateSave(_currentData.Objects[0]);
        //
        //_currentSave!.Reset();
        //if (_currentData.Objects.Count > 0)
        //    ImGui.TextUnformatted(_currentData.Objects[0].Pointer->GameObject.DataID.ToString());
        //VisorBox();
    }

    //private unsafe void VisorBox()
    //{
    //    var (flags, mask) = (_currentSave!.Data.Flags & (ApplicationFlags.SetVisor | ApplicationFlags.Visor)) switch
    //        {
    //            ApplicationFlags.SetVisor                          => (0u, 3u),
    //            ApplicationFlags.Visor                             => (1u, 3u),
    //            ApplicationFlags.SetVisor | ApplicationFlags.Visor => (3u, 3u),
    //            _                                                  => (2u, 3u),
    //        };
    //    var tmp = flags;
    //    if (ImGui.CheckboxFlags("Visor Toggled", ref tmp, mask))
    //    {
    //        _currentSave.Data.Flags = flags switch
    //        {
    //            0 => (_currentSave.Data.Flags | ApplicationFlags.Visor) & ~ApplicationFlags.SetVisor,
    //            1 => _currentSave.Data.Flags | ApplicationFlags.SetVisor,
    //            2 => _currentSave.Data.Flags | ApplicationFlags.SetVisor,
    //            _ => _currentSave.Data.Flags & ~(ApplicationFlags.SetVisor | ApplicationFlags.Visor),
    //        };
    //        if (_currentSave.Data.Flags.HasFlag(ApplicationFlags.SetVisor))
    //        {
    //            var on = _currentSave.Data.Flags.HasFlag(ApplicationFlags.Visor);
    //            foreach (var actor in _currentData.Objects.Where(a => a.IsHuman && a.DrawObject))
    //                RedrawManager.SetVisor(actor.DrawObject.Pointer, on);
    //        }
    //    }
    //}


    //private void DrawActorPanel()
    //{
    //    using var group = ImRaii.Group();
    //    if (!_data.Identifier.IsValid)
    //        return;
    //
    //    if (DrawCustomization(_currentSave.Customize, _currentSave.Equipment, !_data.Modifiable))
    //        //Glamourer.RedrawManager.Set(_data.Actor.Address, _character);
    //        Glamourer.Penumbra.RedrawObject(_data.Actor.Character, RedrawType.Redraw, true);
    //
    //    if (ImGui.Button("Set Machinist Goggles"))
    //        Glamourer.RedrawManager.ChangeEquip(_data.Actor, EquipSlot.Head, new CharacterArmor(265, 1, 0));
    //
    //    if (ImGui.Button("Set Weapon"))
    //        Glamourer.RedrawManager.LoadWeapon(_data.Actor.Address, new CharacterWeapon(0x00C9, 0x004E, 0x0001, 0x00),
    //            new CharacterWeapon(0x0065,                                                     0x003D, 0x0001, 0x00));
    //
    //    if (ImGui.Button("Set Customize"))
    //    {
    //        unsafe
    //        {
    //            var data = _data.Actor.Customize.Data->Clone();
    //            Glamourer.RedrawManager.UpdateCustomize(_data.Actor.DrawObject, new Customize(&data)
    //            {
    //                SkinColor = 154,
    //            });
    //        }
    //    }
    //}
    //
    //private void DrawMonsterPanel()
    //{
    //    using var group        = ImRaii.Group();
    //    var       currentModel = (uint)_data.Actor.ModelId;
    //    var       models       = GameData.Models(Dalamud.GameData);
    //    var       currentData  = models.Models.TryGetValue(currentModel, out var c) ? c.FirstName : $"#{currentModel}";
    //    using var combo        = ImRaii.Combo("Model Id", currentData);
    //    if (!combo)
    //        return;
    //
    //    foreach (var (id, data) in models.Models)
    //    {
    //        if (ImGui.Selectable(data.FirstName, id == currentModel) && id != currentModel)
    //        {
    //            _data.Actor.SetModelId((int)id);
    //            Glamourer.Penumbra.RedrawObject(_data.Actor.Character, RedrawType.Redraw, true);
    //        }
    //
    //        ImGuiUtil.HoverTooltip(data.AllNames);
    //    }
    //}
}
