using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Actors;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Plugin;
using Glamourer.Customization;
using ImGuiNET;
using Penumbra.Api;
using Penumbra.GameData;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Penumbra.PlayerWatch;

namespace Glamourer.Gui
{
    internal partial class Interface : IDisposable
    {
        public const     int    GPoseActorId = 201;
        private const    string PluginName   = "Glamourer";
        private readonly string _glamourerHeader;

        private const float ColorButtonWidth = 22.5f;
        private const float ColorComboWidth  = 140f;

        private readonly IReadOnlyDictionary<byte, Stain>                                       _stains;
        private readonly IReadOnlyDictionary<EquipSlot, List<Item>>                             _equip;
        private readonly ActorTable                                                             _actors;
        private readonly IObjectIdentifier                                                      _identifier;
        private readonly Dictionary<EquipSlot, (ComboWithFilter<Item>, ComboWithFilter<Stain>)> _combos;
        private readonly IPlayerWatcher                                                         _playerWatcher;

        private bool _visible = false;

        private Actor? _player;

        private static readonly Vector2 FeatureIconSizeIntern =
            Vector2.One * ImGui.GetTextLineHeightWithSpacing() * 2 / ImGui.GetIO().FontGlobalScale;

        public static Vector2 FeatureIconSize
            => FeatureIconSizeIntern * ImGui.GetIO().FontGlobalScale;


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

            var stainCombo = new ComboWithFilter<Stain>("##StainCombo", ColorComboWidth, ColorButtonWidth, _stains.Values.ToArray(),
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
                        Vector2.UnitX * (ColorComboWidth - ImGui.GetStyle().ScrollbarSize));
                    ImGui.PopStyleColor(push);
                    return ret;
                },
                ItemsAtOnce = 12,
            };

            _combos = _equip.ToDictionary(kvp => kvp.Key,
                kvp => (new ComboWithFilter<Item>($"{kvp.Key}##Equip", 300, 300, kvp.Value, i => i.Name) { Flags = ImGuiComboFlags.HeightLarge }
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

            if (stainCombo.Draw(string.Empty, out var newStain) && _player != null)
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

        private static readonly string[]
            SubRaceNames = ((SubRace[]) Enum.GetValues(typeof(SubRace))).Skip(1).Select(s => s.ToName()).ToArray();

        private static bool DrawColorPickerPopup(string label, CustomizationSet set, CustomizationId id, out Customization.Customization value)
        {
            value = default;
            if (!ImGui.BeginPopup(label, ImGuiWindowFlags.AlwaysAutoResize))
                return false;

            var ret   = false;
            var count = set.Count(id);
            using var raii = new ImGuiRaii().PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
                .PushStyle(ImGuiStyleVar.FrameRounding, 0);
            for (var i = 0; i < count; ++i)
            {
                var custom = set.Data(id, i);
                if (ImGui.ColorButton($"{i}", ImGui.ColorConvertU32ToFloat4(custom.Color)))
                {
                    value = custom;
                    ret   = true;
                    ImGui.CloseCurrentPopup();
                }

                if (i % 8 != 7)
                    ImGui.SameLine();
            }

            ImGui.EndPopup();
            return ret;
        }

        private static void FixUpAttributes(CustomizationStruct customization)
        {
            var set = GlamourerPlugin.Customization.GetList(customization.Clan, customization.Gender);
            foreach (CustomizationId id in Enum.GetValues(typeof(CustomizationId)))
            {
                switch (id)
                {
                    case CustomizationId.Race:                  break;
                    case CustomizationId.Clan:                  break;
                    case CustomizationId.BodyType:              break;
                    case CustomizationId.Gender:                break;
                    case CustomizationId.FacialFeaturesTattoos: break;
                    case CustomizationId.Face:
                        if (customization.Race != Race.Hrothgar)
                            goto default;
                        break;
                    default:
                        var count = set.Count(id);
                        if (customization[id] >= count)
                            customization[id] = set.Data(id, 0).Value;
                        break;
                }
            }
        }

        private static bool ChangeRace(CustomizationStruct customization, SubRace clan)
        {
            if (clan == customization.Clan)
                return false;

            var race = clan.ToRace();
            customization.Race = race;
            customization.Clan = clan;

            customization.Gender = race switch
            {
                Race.Hrothgar => Gender.Male,
                Race.Viera    => Gender.Female,
                _             => customization.Gender,
            };

            FixUpAttributes(customization);

            return true;
        }

        private static bool ChangeGender(CustomizationStruct customization, Gender gender)
        {
            if (gender == customization.Gender)
                return false;

            customization.Gender = gender;
            FixUpAttributes(customization);

            return true;
        }

        private static bool DrawColorPicker(string label, string tooltip, CustomizationStruct customization, CustomizationId id,
            CustomizationSet set)
        {
            var ret   = false;
            var count = set.Count(id);

            var current = set.DataByValue(id, customization[id], out var custom);
            if (current < 0)
            {
                PluginLog.Warning($"Read invalid customization value {customization[id]} for {id}.");
                current = 0;
                custom  = set.Data(id, 0);
            }

            var popupName = $"Color Picker##{id}";
            if (ImGui.ColorButton($"{current}##color_{id}", ImGui.ColorConvertU32ToFloat4(custom!.Value.Color)))
                ImGui.OpenPopup(popupName);

            ImGui.SameLine();
            ImGui.SetNextItemWidth(50 + 2 * 22.5f * ImGui.GetIO().FontGlobalScale);
            if (ImGui.InputInt($"##text_{id}", ref current, 1) && current != customization[id] && current >= 0 && current < count)
            {
                customization[id] = set.Data(id, current).Value;
                ret               = true;
            }

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip($"Input Range: [0, {count - 1}]");

            ImGui.SameLine();
            ImGui.Text(label);
            if (tooltip.Any() && ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);

            if (!DrawColorPickerPopup(popupName, set, id, out var newCustom))
                return ret;

            customization[id] = newCustom.Value;
            ret               = true;

            return ret;
        }

        private static bool DrawListSelector(string label, string tooltip, CustomizationStruct customization, CustomizationId id,
            CustomizationSet set)
        {
            var ret     = false;
            int current = customization[id];
            var count   = set.Count(id);

            ImGui.SetNextItemWidth(150 * ImGui.GetIO().FontGlobalScale);
            if (ImGui.BeginCombo($"##combo_{id}", $"{id} #{current + 1}"))
            {
                for (var i = 0; i < count; ++i)
                {
                    if (ImGui.Selectable($"{id} #{i + 1}##combo", i == current) && i != current)
                    {
                        customization[id] = (byte) i;
                        ret               = true;
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(50 + 2 * 22.5f * ImGui.GetIO().FontGlobalScale);
            if (ImGui.InputInt($"##text_{id}", ref current, 1) && current != customization[id] && current >= 0 && current < count)
            {
                customization[id] = set.Data(id, current).Value;
                ret               = true;
            }

            ImGui.SameLine();
            ImGui.Text(label);
            if (tooltip.Any() && ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);

            return ret;
        }

        private static readonly Vector4 NoColor  = new(1f, 1f, 1f, 1f);
        private static readonly Vector4 RedColor = new(0.6f, 0.3f, 0.3f, 1f);

        private static bool DrawMultiSelector(CustomizationStruct customization, CustomizationSet set)
        {
            var       ret   = false;
            var       count = set.Count(CustomizationId.FacialFeaturesTattoos);
            using var raii  = new ImGuiRaii().PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing / 2);
            for (var i = 0; i < count; ++i)
            {
                var enabled = customization.FacialFeature(i);
                var feature = set.FacialFeature(set.Race == Race.Hrothgar ? customization.Hairstyle : customization.Face, i);
                var icon    = GlamourerPlugin.Customization.GetIcon(feature.IconId);
                if (ImGui.ImageButton(icon.ImGuiHandle, FeatureIconSize, Vector2.Zero, Vector2.One, (int) ImGui.GetStyle().FramePadding.X,
                    Vector4.Zero,
                    enabled ? NoColor : RedColor))
                {
                    ret = true;
                    customization.FacialFeature(i, !enabled);
                }

                if (ImGui.IsItemHovered())
                {
                    using var tt = ImGuiRaii.NewTooltip();
                    ImGui.Image(icon.ImGuiHandle, new Vector2(icon.Width, icon.Height));
                }

                ImGui.SameLine();
            }

            raii.PopStyles();
            raii.Group();
            int value = customization[CustomizationId.FacialFeaturesTattoos];
            ImGui.SetNextItemWidth(50 + 2 * 22.5f * ImGui.GetIO().FontGlobalScale);
            if (ImGui.InputInt($"##{CustomizationId.FacialFeaturesTattoos}", ref value, 1)
             && value != customization[CustomizationId.FacialFeaturesTattoos]
             && value > 0
             && value < 256)
            {
                customization[CustomizationId.FacialFeaturesTattoos] = (byte) value;
                ret                                                  = true;
            }

            ImGui.Text("Facial Features & Tattoos");


            return ret;
        }

        private static bool DrawIconPickerPopup(string label, CustomizationSet set, CustomizationId id, out Customization.Customization value)
        {
            value = default;
            if (!ImGui.BeginPopup(label, ImGuiWindowFlags.AlwaysAutoResize))
                return false;

            var ret   = false;
            var count = set.Count(id);
            using var raii = new ImGuiRaii().PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
                .PushStyle(ImGuiStyleVar.FrameRounding, 0);
            for (var i = 0; i < count; ++i)
            {
                var custom = set.Data(id, i);
                var icon   = GlamourerPlugin.Customization.GetIcon(custom.IconId);
                if (ImGui.ImageButton(icon.ImGuiHandle, FeatureIconSize))
                {
                    value = custom;
                    ret   = true;
                    ImGui.CloseCurrentPopup();
                }

                if (ImGui.IsItemHovered())
                {
                    using var tt = ImGuiRaii.NewTooltip();
                    ImGui.Image(icon.ImGuiHandle, new Vector2(icon.Width, icon.Height));
                }

                if (i % 8 != 7)
                    ImGui.SameLine();
            }

            ImGui.EndPopup();
            return ret;
        }

        private static bool DrawIconSelector(string label, string tooltip, CustomizationStruct customization, CustomizationId id,
            CustomizationSet set)
        {
            var ret   = false;
            var count = set.Count(id);

            var current = set.DataByValue(id, customization[id], out var custom);
            if (current < 0)
            {
                PluginLog.Warning($"Read invalid customization value {customization[id]} for {id}.");
                current = 0;
                custom  = set.Data(id, 0);
            }

            var popupName = $"Style Picker##{id}";
            var icon      = GlamourerPlugin.Customization.GetIcon(custom!.Value.IconId);
            if (ImGui.ImageButton(icon.ImGuiHandle, FeatureIconSize))
                ImGui.OpenPopup(popupName);

            if (ImGui.IsItemHovered())
            {
                using var tt = ImGuiRaii.NewTooltip();
                ImGui.Image(icon.ImGuiHandle, new Vector2(icon.Width, icon.Height));
            }

            ImGui.SameLine();
            using var group = ImGuiRaii.NewGroup();
            ImGui.SetNextItemWidth(50 + 2 * 22.5f * ImGui.GetIO().FontGlobalScale);
            var oldIdx = current;
            if (ImGui.InputInt($"##text_{id}", ref current, 1) && current != oldIdx && current >= 0 && current < count)
            {
                customization[id] = set.Data(id, current).Value;
                ret               = true;
            }

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip($"Input Range: [0, {count - 1}]");

            if (DrawIconPickerPopup(popupName, set, id, out var newCustom))
            {
                customization[id] = newCustom.Value;
                ret               = true;
            }

            ImGui.Text(label);
            if (tooltip.Any() && ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);

            return ret;
        }


        private static bool DrawPercentageSelector(string label, string tooltip, CustomizationStruct customization, CustomizationId id,
            CustomizationSet set)
        {
            var ret   = false;
            int value = customization[id];
            var count = set.Count(id);
            ImGui.SetNextItemWidth(150 * ImGui.GetIO().FontGlobalScale);
            if (ImGui.SliderInt($"##slider_{id}", ref value, 0, count - 1, "") && value != customization[id])
            {
                customization[id] = (byte) value;
                ret               = true;
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(50 + 2 * 22.5f * ImGui.GetIO().FontGlobalScale);
            if (ImGui.InputInt($"##input_{id}", ref value, 1) && value != customization[id] && value >= 0 && value < count)
            {
                customization[id] = (byte) value;
                ret               = true;
            }

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip($"Input Range: [0, {count - 1}]");

            ImGui.SameLine();
            ImGui.Text(label);
            if (tooltip.Any() && ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);

            return ret;
        }

        private bool DrawStuff()
        {
            var ret = false;
            var x   = new CustomizationStruct(_player!.Address + 0x1898);
            _currentSubRace = x.Clan;
            ImGui.SetNextItemWidth(150 * ImGui.GetIO().FontGlobalScale);
            if (ImGui.BeginCombo("SubRace", SubRaceNames[(int) _currentSubRace - 1]))
            {
                for (var i = 0; i < SubRaceNames.Length; ++i)
                {
                    if (ImGui.Selectable(SubRaceNames[i], (int) _currentSubRace == i + 1))
                    {
                        _currentSubRace =  (SubRace) i + 1;
                        ret             |= ChangeRace(x, _currentSubRace);
                    }
                }

                ImGui.EndCombo();
            }

            _currentGender = x.Gender;
            ImGui.SetNextItemWidth(150 * ImGui.GetIO().FontGlobalScale);
            if (ImGui.BeginCombo("Gender", _currentGender.ToName()))
            {
                if (_currentSubRace.ToRace() != Race.Viera
                 && ImGui.Selectable(Gender.Male.ToName(), _currentGender == Gender.Male)
                 && _currentGender != Gender.Male)
                {
                    _currentGender = Gender.Male;
                    ret            = ChangeGender(x, _currentGender);
                }

                if (_currentSubRace.ToRace() != Race.Hrothgar
                 && ImGui.Selectable(Gender.Female.ToName(), _currentGender == Gender.Female)
                 && _currentGender != Gender.Female)
                {
                    _currentGender = Gender.Female;
                    ret            = ChangeGender(x, _currentGender);
                }

                ImGui.EndCombo();
            }

            var set = GlamourerPlugin.Customization.GetList(_currentSubRace, _currentGender);


            foreach (CustomizationId customizationId in Enum.GetValues(typeof(CustomizationId)))
            {
                if (!set.IsAvailable(customizationId))
                    continue;

                switch (customizationId.ToType(_currentSubRace.ToRace() == Race.Hrothgar))
                {
                    case CharaMakeParams.MenuType.ColorPicker:
                        ret |= DrawColorPicker(customizationId.ToString(), "", x,
                            customizationId,                               set);
                        break;
                    case CharaMakeParams.MenuType.ListSelector:
                        ret |= DrawListSelector(customizationId.ToString(), "", x,
                            customizationId,                                set);
                        break;
                    case CharaMakeParams.MenuType.IconSelector:
                        ret |= DrawIconSelector(customizationId.ToString(), "", x, customizationId, set);
                        break;
                    case CharaMakeParams.MenuType.MultiIconSelector:
                        ret |= DrawMultiSelector(x, set);
                        break;
                    case CharaMakeParams.MenuType.Percentage:
                        ret |= DrawPercentageSelector(customizationId.ToString(), "", x, customizationId, set);
                        break;
                }
            }

            return ret;
        }

        private void Draw()
        {
            ImGui.SetNextWindowSizeConstraints(Vector2.One * 600, Vector2.One * 5000);
            if (!_visible || !ImGui.Begin(_glamourerHeader, ref _visible))
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

                    changes |= DrawStuff();

                    if (changes)
                        UpdateActors(_player);
                }
            }
            finally
            {
                ImGui.End();
            }
        }
    }
}
