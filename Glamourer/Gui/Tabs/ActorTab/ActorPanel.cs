using System.Numerics;
using Glamourer.Gui.Customization;
using Glamourer.Interop.Structs;
using Glamourer.State;
using ImGuiNET;
using OtterGui.Raii;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;

namespace Glamourer.Gui.Tabs.ActorTab;

public class ActorPanel
{
    private readonly ActorSelector       _selector;
    private readonly StateManager        _stateManager;
    private readonly CustomizationDrawer _customizationDrawer;

    private ActorIdentifier _identifier;
    private string          _actorName = string.Empty;
    private Actor           _actor     = Actor.Null;
    private ActorData       _data;
    private ActorState?     _state;

    public ActorPanel(ActorSelector selector, StateManager stateManager, CustomizationDrawer customizationDrawer)
    {
        _selector            = selector;
        _stateManager        = stateManager;
        _customizationDrawer = customizationDrawer;
    }

    public void Draw()
    {
        if (!_selector.HasSelection)
            return;

        (_identifier, _data) = _selector.Selection;
        if (_data.Valid)
        {
            _actorName = _data.Label;
            _actor     = _data.Objects[0];
        }
        else
        {
            _actorName = _identifier.ToString();
            _actor     = Actor.Null;
        }

        if (!_stateManager.GetOrCreate(_identifier, _actor, out _state))
            return;

        //if (_state != null)
        //    _stateManager.Update(ref _state.Data, _actor);

        using var group = ImRaii.Group();
        DrawHeader();
        DrawPanel();
    }

    private void DrawHeader()
    {
        var color       = _data.Valid ? ColorId.ActorAvailable.Value() : ColorId.ActorUnavailable.Value();
        var buttonColor = ImGui.GetColorU32(ImGuiCol.FrameBg);
        using var c = ImRaii.PushColor(ImGuiCol.Text, color)
            .Push(ImGuiCol.Button,        buttonColor)
            .Push(ImGuiCol.ButtonHovered, buttonColor)
            .Push(ImGuiCol.ButtonActive,  buttonColor);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
            .Push(ImGuiStyleVar.FrameRounding, 0);
        ImGui.Button($"{(_data.Valid ? _data.Label : _identifier.ToString())}##playerHeader", -Vector2.UnitX);
    }

    private unsafe void DrawPanel()
    {
        using var child = ImRaii.Child("##ActorPanel", -Vector2.One, true);
        if (!child || _state == null)
            return;

        if (_customizationDrawer.Draw(_state.ModelData.Customize, false))
        {
        }
        // if (_currentData.Valid)
        //     _currentSave.Initialize(_items, _currentData.Objects[0]);
        // 
        // RevertButton();
        // ActorDebug.Draw(_currentSave.ModelData);
        // return;
        // 
        // if (_main._customizationDrawer.Draw(_currentSave.ModelData.Customize, _identifier.Type == IdentifierType.Special))
        //     _activeDesigns.ChangeCustomize(_currentSave, _main._customizationDrawer.Changed, _main._customizationDrawer.Customize.Data,
        //         false);
        // 
        // foreach (var slot in EquipSlotExtensions.EqdpSlots)
        // {
        //     var current = _currentSave.Armor(slot);
        //     if (_main._equipmentDrawer.DrawStain(current.Stain, slot, out var stain))
        //         _activeDesigns.ChangeStain(_currentSave, slot, stain.RowIndex, false);
        //     ImGui.SameLine();
        //     if (_main._equipmentDrawer.DrawArmor(current, slot, out var armor, _currentSave.ModelData.Customize.Gender,
        //             _currentSave.ModelData.Customize.Race))
        //         _activeDesigns.ChangeEquipment(_currentSave, slot, armor, false);
        // }
        // 
        // var currentMain = _currentSave.WeaponMain;
        // if (_main._equipmentDrawer.DrawStain(currentMain.Stain, EquipSlot.MainHand, out var stainMain))
        //     _activeDesigns.ChangeStain(_currentSave, EquipSlot.MainHand, stainMain.RowIndex, false);
        // ImGui.SameLine();
        // _main._equipmentDrawer.DrawMainhand(currentMain, true, out var main);
        // if (currentMain.Type.Offhand() != FullEquipType.Unknown)
        // {
        //     var currentOff = _currentSave.WeaponOff;
        //     if (_main._equipmentDrawer.DrawStain(currentOff.Stain, EquipSlot.OffHand, out var stainOff))
        //         _activeDesigns.ChangeStain(_currentSave, EquipSlot.OffHand, stainOff.RowIndex, false);
        //     ImGui.SameLine();
        //     _main._equipmentDrawer.DrawOffhand(currentOff, main.Type, out var off);
        // }
        // 
        // if (_main._equipmentDrawer.DrawVisor(_currentSave, out var value))
        //     _activeDesigns.ChangeVisor(_currentSave, value, false);
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
