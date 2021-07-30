using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Data.LuminaExtensions;
using Dalamud.Game.ClientState.Actors;
using Dalamud.Game.ClientState.Actors.Types;
using Glamourer.Customization;
using ImGuiNET;
using Penumbra.Api;
using Penumbra.GameData;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Penumbra.PlayerWatch;
using SDL2;

namespace Glamourer.Gui
{
    internal class Interface : IDisposable
    {
        public const     int    GPoseActorId = 201;
        private const    string PluginName   = "Glamourer";
        private readonly string _glamourerHeader;

        private const int ColorButtonWidth = 140;

        private readonly IReadOnlyDictionary<byte, Stain>                                       _stains;
        private readonly IReadOnlyDictionary<EquipSlot, List<Item>>                             _equip;
        private readonly ActorTable                                                             _actors;
        private readonly IObjectIdentifier                                                      _identifier;
        private readonly Dictionary<EquipSlot, (ComboWithFilter<Item>, ComboWithFilter<Stain>)> _combos;
        private readonly IPlayerWatcher                                                         _playerWatcher;

        private bool _visible = false;

        private Actor? _player;

        private static readonly Vector2 FeatureIconSize = new(80, 80);


        public Interface()
        {
            _glamourerHeader = GlamourerPlugin.Version.Length > 0
                ? $"{PluginName} v{GlamourerPlugin.Version}###{PluginName}Main"
                : $"{PluginName}###{PluginName}Main";
            GlamourerPlugin.PluginInterface.UiBuilder.OnBuildUi      += Draw;
            GlamourerPlugin.PluginInterface.UiBuilder.OnOpenConfigUi += ToggleVisibility;

            _stains        = GameData.Stains(GlamourerPlugin.PluginInterface);
            _equip         = GameData.ItemsBySlot(GlamourerPlugin.PluginInterface);
            _identifier    = Penumbra.GameData.GameData.GetIdentifier(GlamourerPlugin.PluginInterface);
            _actors        = GlamourerPlugin.PluginInterface.ClientState.Actors;
            _playerWatcher = PlayerWatchFactory.Create(GlamourerPlugin.PluginInterface);

            var stainCombo = new ComboWithFilter<Stain>("##StainCombo", ColorButtonWidth, _stains.Values.ToArray(),
                s => s.Name.ToString())
            {
                Flags = ImGuiComboFlags.NoArrowButton | ImGuiComboFlags.HeightLarge,
                PreList = () =>
                {
                    ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing,   Vector2.Zero);
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
                    ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);
                },
                PostList = () => { ImGui.PopStyleVar(3); },
                CreateSelectable = s =>
                {
                    var push = PushColor(s);
                    var ret = ImGui.Button($"{s.Name}##Stain{(byte) s.RowIndex}",
                        Vector2.UnitX * (ColorButtonWidth - ImGui.GetStyle().ScrollbarSize));
                    ImGui.PopStyleColor(push);
                    return ret;
                },
                ItemsAtOnce = 12,
            };

            _combos = _equip.ToDictionary(kvp => kvp.Key,
                kvp => (new ComboWithFilter<Item>($"{kvp.Key}##Equip", 300, kvp.Value, i => i.Name) { Flags = ImGuiComboFlags.HeightLarge }
                    , new ComboWithFilter<Stain>($"##{kvp.Key}Stain", stainCombo))
            );
        }

        public void ToggleVisibility(object _, object _2)
            => _visible = !_visible;

        public void Dispose()
        {
            _playerWatcher?.Dispose();
            GlamourerPlugin.PluginInterface.UiBuilder.OnBuildUi      -= Draw;
            GlamourerPlugin.PluginInterface.UiBuilder.OnOpenConfigUi -= ToggleVisibility;
        }

        private string _currentActorName = "";

        private static int PushColor(Stain stain, ImGuiCol type = ImGuiCol.Button)
        {
            ImGui.PushStyleColor(type, stain.RgbaColor);
            if (stain.Intensity > 127)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, 0xFF101010);
                return 2;
            }

            return 1;
        }

        private bool DrawColorSelector(ComboWithFilter<Stain> stainCombo, EquipSlot slot, StainId stainIdx)
        {
            var name = string.Empty;
            stainCombo.PostPreview = null;
            if (_stains.TryGetValue((byte) stainIdx, out var stain))
            {
                name = stain.Name;
                var previewPush = PushColor(stain, ImGuiCol.FrameBg);
                stainCombo.PostPreview = () => ImGui.PopStyleColor(previewPush);
            }

            if (stainCombo.Draw(name, out var newStain) && _player != null)
            {
                newStain.Write(_player.Address, slot);
                return true;
            }

            return false;
        }

        private bool DrawItemSelector(ComboWithFilter<Item> equipCombo, Lumina.Excel.GeneratedSheets.Item? item)
        {
            var currentName = item?.Name.ToString() ?? "Nothing";
            if (equipCombo.Draw(currentName, out var newItem) && _player != null)
            {
                newItem.Write(_player.Address);
                return true;
            }

            return false;
        }

        private bool DrawEquip(EquipSlot slot, ActorArmor equip)
        {
            var (equipCombo, stainCombo) = _combos[slot];

            var ret = false;
            ret = DrawColorSelector(stainCombo, slot, equip.Stain);
            ImGui.SameLine();
            var item = _identifier.Identify(equip.Set, new WeaponType(), equip.Variant, slot);
            ret |= DrawItemSelector(equipCombo, item);

            return ret;
        }

        private bool DrawWeapon(EquipSlot slot, ActorWeapon weapon)
        {
            var (equipCombo, stainCombo) = _combos[slot];

            var ret = DrawColorSelector(stainCombo, slot, weapon.Stain);
            ImGui.SameLine();


            var item = _identifier.Identify(weapon.Set, weapon.Type, weapon.Variant, slot);
            ret |= DrawItemSelector(equipCombo, item);

            return ret;
        }

        public void UpdateActors(Actor actor)
        {
            var newEquip = _playerWatcher.UpdateActorWithoutEvent(actor);
            GlamourerPlugin.Penumbra?.RedrawActor(actor, RedrawType.WithSettings);

            var gPose  = _actors[GPoseActorId];
            var player = _actors[0];
            if (gPose != null && actor.Address == gPose.Address && player != null)
                newEquip.Write(player.Address);
        }

        private SubRace         _currentSubRace       = SubRace.Midlander;
        private Gender          _currentGender        = Gender.Male;
        private CustomizationId _currentCustomization = CustomizationId.Hairstyle;

        private static readonly string[]
            SubRaceNames = ((SubRace[]) Enum.GetValues(typeof(SubRace))).Skip(1).Select(s => s.ToName()).ToArray();


        private void DrawStuff()
        {
            if (ImGui.BeginCombo("SubRace", _currentSubRace.ToString()))
            {
                for (var i = 0; i < SubRaceNames.Length; ++i)
                {
                    if (ImGui.Selectable(SubRaceNames[i], (int) _currentSubRace == i + 1))
                        _currentSubRace = (SubRace) (i + 1);
                }

                ImGui.EndCombo();
            }

            if (ImGui.BeginCombo("Gender", _currentGender.ToName()))
            {
                if (ImGui.Selectable(Gender.Male.ToName(), _currentGender == Gender.Male))
                    _currentGender = Gender.Male;
                if (ImGui.Selectable(Gender.Female.ToName(), _currentGender == Gender.Female))
                    _currentGender = Gender.Female;
                ImGui.EndCombo();
            }


            var set = GlamourerPlugin.Customization.GetList(_currentSubRace, _currentGender);
            if (ImGui.BeginCombo("Customization", _currentCustomization.ToString()))
            {
                foreach (CustomizationId customizationId in Enum.GetValues(typeof(CustomizationId)))
                {
                    if (!set.IsAvailable(customizationId))
                        continue;

                    if (ImGui.Selectable(customizationId.ToString(), customizationId == _currentCustomization))
                        _currentCustomization = customizationId;
                }

                ImGui.EndCombo();
            }

            var count = set.Count(_currentCustomization);
            var tmp   = 0;
            switch (_currentCustomization.ToType(_currentSubRace.ToRace() == Race.Hrothgar))
            {
                case CharaMakeParams.MenuType.ColorPicker:
                {
                    using var raii = new ImGuiRaii().PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
                        .PushStyle(ImGuiStyleVar.FrameRounding, 0f);
                    for (var i = 0; i < count; ++i)
                    {
                        var data = set.Data(_currentCustomization, i);
                        ImGui.ColorButton($"{data.Value}", ImGui.ColorConvertU32ToFloat4(data.Color));
                        if (i % 8 != 7)
                            ImGui.SameLine();
                    }
                }
                    break;
                case CharaMakeParams.MenuType.Percentage:
                    ImGui.SliderInt("Percentage", ref tmp, 0, 100);
                    break;
                case CharaMakeParams.MenuType.ListSelector:
                    ImGui.Combo("List", ref tmp, Enumerable.Range(0, count).Select(i => $"{_currentCustomization} #{i}").ToArray(), count);
                    break;
                case CharaMakeParams.MenuType.IconSelector:
                case CharaMakeParams.MenuType.MultiIconSelector:
                {
                    using var raii = new ImGuiRaii().PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
                        .PushStyle(ImGuiStyleVar.FrameRounding, 0f);
                    for (var i = 0; i < count; ++i)
                    {
                        var data    = set.Data(_currentCustomization, i);
                        var texture = GlamourerPlugin.Customization.GetIcon(data.IconId);
                        ImGui.ImageButton(texture.ImGuiHandle, FeatureIconSize * ImGui.GetIO().FontGlobalScale);
                        if (ImGui.IsItemHovered())
                        {
                            using var tooltip = ImGuiRaii.NewTooltip();
                            ImGui.Image(texture.ImGuiHandle, new Vector2(texture.Width, texture.Height));
                        }

                        if (i % 4 != 3)
                            ImGui.SameLine();
                    }
                }
                    break;
            }
        }

        private void Draw()
        {
            ImGui.SetNextWindowSizeConstraints(Vector2.One * 600, Vector2.One * 5000);
            if (!_visible || !ImGui.Begin(_glamourerHeader))
                return;

            try
            {
                if (ImGui.BeginCombo("Actor", _currentActorName))
                {
                    var idx = 0;
                    foreach (var actor in GlamourerPlugin.PluginInterface.ClientState.Actors.Where(a => a.ObjectKind == ObjectKind.Player))
                    {
                        if (ImGui.Selectable($"{actor.Name}##{idx++}"))
                            _currentActorName = actor.Name;
                    }

                    ImGui.EndCombo();
                }

                _player = _actors[GPoseActorId] ?? _actors[0];
                if (_player == null || !GlamourerPlugin.PluginInterface.ClientState.Condition.Any())
                {
                    ImGui.TextColored(new Vector4(0.4f, 0.1f, 0.1f, 1f),
                        "No player character available.");
                }
                else
                {
                    var equip = new ActorEquipment(_player);

                    var changes = false;
                    changes |= DrawWeapon(EquipSlot.MainHand, equip.MainHand);
                    changes |= DrawWeapon(EquipSlot.OffHand,  equip.OffHand);
                    changes |= DrawEquip(EquipSlot.Head,    equip.Head);
                    changes |= DrawEquip(EquipSlot.Body,    equip.Body);
                    changes |= DrawEquip(EquipSlot.Hands,   equip.Hands);
                    changes |= DrawEquip(EquipSlot.Legs,    equip.Legs);
                    changes |= DrawEquip(EquipSlot.Feet,    equip.Feet);
                    changes |= DrawEquip(EquipSlot.Ears,    equip.Ears);
                    changes |= DrawEquip(EquipSlot.Neck,    equip.Neck);
                    changes |= DrawEquip(EquipSlot.Wrists,  equip.Wrists);
                    changes |= DrawEquip(EquipSlot.RFinger, equip.RFinger);
                    changes |= DrawEquip(EquipSlot.LFinger, equip.LFinger);

                    if (changes)
                        UpdateActors(_player);
                }

                DrawStuff();
            }
            finally
            {
                ImGui.End();
            }
        }
    }
}
