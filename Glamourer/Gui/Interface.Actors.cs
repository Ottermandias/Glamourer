using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Interface;
using Glamourer.Interop;
using Glamourer.Services;
using Glamourer.State;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;
using ImGui = ImGuiNET.ImGui;

namespace Glamourer.Gui;

public partial class Interface
{
    private class ActorTab
    {
        private readonly Interface            _main;
        private readonly ActiveDesign.Manager _activeDesigns;
        private readonly ObjectManager        _objects;
        private readonly TargetManager        _targets;
        private readonly ActorService         _actors;
        private readonly ItemManager          _items;

        public ActorTab(Interface main, ActiveDesign.Manager activeDesigns, ObjectManager objects, TargetManager targets, ActorService actors,
            ItemManager items)
        {
            _main          = main;
            _activeDesigns = activeDesigns;
            _objects       = objects;
            _targets       = targets;
            _actors        = actors;
            _items         = items;
        }

        private ActorIdentifier _identifier   = ActorIdentifier.Invalid;
        private ActorData       _currentData  = ActorData.Invalid;
        private ActiveDesign?   _currentSave;

        public void Draw()
        {
            using var tab = ImRaii.TabItem("Actors");
            if (!tab)
                return;

            DrawActorSelector();
            if (!_objects.TryGetValue(_identifier, out _currentData))
                _currentData = ActorData.Invalid;

            ImGui.SameLine();

            DrawPanel();
        }

        private unsafe void DrawPanel()
        {
            if (_identifier == ActorIdentifier.Invalid)
                return;

            using var group = ImRaii.Group();
            DrawPanelHeader();
            using var child = ImRaii.Child("##ActorPanel", -Vector2.One, true);
            if (!child || _currentSave == null)
                return;

            if (_currentData.Valid)
                _currentSave.Initialize(_items, _currentData.Objects[0]);

            RevertButton();
            if (_main._customizationDrawer.Draw(_currentSave.Customize, _identifier.Type == IdentifierType.Special))
                _activeDesigns.ChangeCustomize(_currentSave, _main._customizationDrawer.Changed, _main._customizationDrawer.CustomizeData,
                    false);

            foreach (var slot in EquipSlotExtensions.EqdpSlots)
            {
                var current = _currentSave.Armor(slot);
                if (_main._equipmentDrawer.DrawStain(current.Stain, slot, out var stain))
                    _activeDesigns.ChangeStain(_currentSave, slot, stain.RowIndex, false);
                ImGui.SameLine();
                if (_main._equipmentDrawer.DrawArmor(current, slot, out var armor, _currentSave.Customize.Gender,
                        _currentSave.Customize.Race))
                    _activeDesigns.ChangeEquipment(_currentSave, slot, armor, false);
            }

            var currentMain = _currentSave.WeaponMain;
            if (_main._equipmentDrawer.DrawStain(currentMain.Stain, EquipSlot.MainHand, out var stainMain))
                _activeDesigns.ChangeStain(_currentSave, EquipSlot.MainHand, stainMain.RowIndex, false);
            ImGui.SameLine();
            _main._equipmentDrawer.DrawMainhand(currentMain, true, out var main);
            if (currentMain.Type.Offhand() != FullEquipType.Unknown)
            {
                var currentOff = _currentSave.WeaponOff;
                if (_main._equipmentDrawer.DrawStain(currentOff.Stain, EquipSlot.OffHand, out var stainOff))
                    _activeDesigns.ChangeStain(_currentSave, EquipSlot.OffHand, stainOff.RowIndex, false);
                ImGui.SameLine();
                _main._equipmentDrawer.DrawOffhand(currentOff, main.Type, out var off);
            }

            if (_main._equipmentDrawer.DrawVisor(_currentSave, out var value))
                _activeDesigns.ChangeVisor(_currentSave, value, false);
        }

        private const uint RedHeaderColor   = 0xFF1818C0;
        private const uint GreenHeaderColor = 0xFF18C018;

        private unsafe void RevertButton()
        {
            if (ImGui.Button("Revert"))
                _activeDesigns.RevertDesign(_currentSave!);
            //foreach (var actor in _currentData.Objects)
            //    _currentSave!.ApplyToActor(actor);
            //
            //if (_currentData.Objects.Count > 0)
            //    _currentSave = _manipulations.GetOrCreateSave(_currentData.Objects[0]);
            //
            //_currentSave!.Reset();
            if (_currentData.Objects.Count > 0)
                ImGui.TextUnformatted(_currentData.Objects[0].Pointer->GameObject.DataID.ToString());
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

        private void DrawPanelHeader()
        {
            var color       = _currentData.Valid ? GreenHeaderColor : RedHeaderColor;
            var buttonColor = ImGui.GetColorU32(ImGuiCol.FrameBg);
            using var c = ImRaii.PushColor(ImGuiCol.Text, color)
                .Push(ImGuiCol.Button,        buttonColor)
                .Push(ImGuiCol.ButtonHovered, buttonColor)
                .Push(ImGuiCol.ButtonActive,  buttonColor);
            using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
                .Push(ImGuiStyleVar.FrameRounding, 0);
            ImGui.Button($"{_currentData.Label}##playerHeader", -Vector2.UnitX);
        }

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


        private LowerString _actorFilter = LowerString.Empty;

        private void DrawActorSelector()
        {
            using var group      = ImRaii.Group();
            var       oldSpacing = ImGui.GetStyle().ItemSpacing;
            using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
                .Push(ImGuiStyleVar.FrameRounding, 0);
            ImGui.SetNextItemWidth(_actorSelectorWidth);
            LowerString.InputWithHint("##actorFilter", "Filter...", ref _actorFilter, 64);

            DrawSelector(oldSpacing);
            DrawSelectionButtons();
        }

        private void DrawSelector(Vector2 oldSpacing)
        {
            using var child = ImRaii.Child("##actorSelector", new Vector2(_actorSelectorWidth, -ImGui.GetFrameHeight()), true);
            if (!child)
                return;

            _objects.Update();
            if (!_activeDesigns.TryGetValue(_identifier, out _currentSave))
                _currentSave = null;

            using var style     = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, oldSpacing);
            var       skips     = ImGuiClip.GetNecessarySkips(ImGui.GetTextLineHeight());
            var       remainder = ImGuiClip.FilteredClippedDraw(_objects, skips, CheckFilter, DrawSelectable);
            ImGuiClip.DrawEndDummy(remainder, ImGui.GetTextLineHeight());
            if (_currentSave == null)
            {
                _identifier = ActorIdentifier.Invalid;
                _currentData = ActorData.Invalid;
            }
        }

        private bool CheckFilter(KeyValuePair<ActorIdentifier, ActorData> pair)
            => _actorFilter.IsEmpty || pair.Value.Label.Contains(_actorFilter.Lower, StringComparison.OrdinalIgnoreCase);

        private void DrawSelectable(KeyValuePair<ActorIdentifier, ActorData> pair)
        {
            var equal = pair.Key.Equals(_identifier);
            if (ImGui.Selectable(pair.Value.Label, equal) || equal)
            {
                _identifier  = pair.Key.CreatePermanent();
                _currentData = pair.Value;
                if (!_activeDesigns.TryGetValue(_identifier, out _currentSave))
                    _currentSave = _currentData.Valid ? _activeDesigns.GetOrCreateSave(_currentData.Objects[0]) : null;
            }
        }

        private void DrawSelectionButtons()
        {
            using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
                .Push(ImGuiStyleVar.FrameRounding, 0);
            var buttonWidth = new Vector2(_actorSelectorWidth / 2, 0);

            if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.UserCircle.ToIconString(), buttonWidth
                    , "Select the local player character.", !_objects.Player, true))
                _identifier = _objects.Player.GetIdentifier(_actors.AwaitedService);

            ImGui.SameLine();
            Actor targetActor = _targets.Target?.Address;
            if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.HandPointer.ToIconString(), buttonWidth,
                    "Select the current target, if it is in the list.", _objects.IsInGPose || !targetActor, true))
                _identifier = targetActor.GetIdentifier(_actors.AwaitedService);
        }
    }
}
