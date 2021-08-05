using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Dalamud.Game.ClientState.Actors;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Interface;
using Dalamud.Plugin;
using Glamourer.Customization;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Penumbra.Api;
using Penumbra.GameData;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Penumbra.PlayerWatch;
using Race = Penumbra.GameData.Enums.Race;

namespace Glamourer.Gui
{
    internal partial class Interface
    {
        // Push the stain color to type and if it is too bright, turn the text color black.
        // Return number of pushed styles.
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

        // Update actors without triggering PlayerWatcher Events,
        // then manually redraw using Penumbra.
        public void UpdateActors(Actor actor)
        {
            var newEquip = _playerWatcher.UpdateActorWithoutEvent(actor);
            GlamourerPlugin.Penumbra?.RedrawActor(actor, RedrawType.WithSettings);

            // Special case for carrying over changes to the gPose actor to the regular player actor, too.
            var gPose  = _actors[GPoseActorId];
            var player = _actors[0];
            if (gPose != null && actor.Address == gPose.Address && player != null)
                newEquip.Write(player.Address);
        }

        // Go through a whole customization struct and fix up all settings that need fixing.
        private static void FixUpAttributes(LazyCustomization customization)
        {
            var set = GlamourerPlugin.Customization.GetList(customization.Value.Clan, customization.Value.Gender);
            foreach (CustomizationId id in Enum.GetValues(typeof(CustomizationId)))
            {
                switch (id)
                {
                    case CustomizationId.Race:                  break;
                    case CustomizationId.Clan:                  break;
                    case CustomizationId.BodyType:              break;
                    case CustomizationId.Gender:                break;
                    case CustomizationId.FacialFeaturesTattoos: break;
                    case CustomizationId.HighlightsOnFlag:      break;
                    case CustomizationId.Face:
                        if (customization.Value.Race != Race.Hrothgar)
                            goto default;
                        break;
                    default:
                        var count = set.Count(id);
                        if (customization.Value[id] >= count)
                            if (count == 0)
                                customization.Value[id] = 0;
                            else
                                customization.Value[id] = set.Data(id, 0).Value;
                        break;
                }
            }
        }

        // Change a race and fix up all required customizations afterwards.
        private static bool ChangeRace(LazyCustomization customization, SubRace clan)
        {
            if (clan == customization.Value.Clan)
                return false;

            var race = clan.ToRace();
            customization.Value.Race = race;
            customization.Value.Clan = clan;

            customization.Value.Gender = race switch
            {
                Race.Hrothgar => Gender.Male,
                Race.Viera    => Gender.Female,
                _             => customization.Value.Gender,
            };

            FixUpAttributes(customization);

            return true;
        }

        // Change a gender and fix up all required customizations afterwards.
        private static bool ChangeGender(LazyCustomization customization, Gender gender)
        {
            if (gender == customization.Value.Gender)
                return false;

            customization.Value.Gender = gender;
            FixUpAttributes(customization);

            return true;
        }
    }

    internal partial class Interface
    {
        private const float ColorButtonWidth = 22.5f;
        private const float ColorComboWidth  = 140f;
        private const float ItemComboWidth   = 300f;

        private ComboWithFilter<Stain> CreateDefaultStainCombo(IReadOnlyList<Stain> stains)
            => new("##StainCombo", ColorComboWidth, ColorButtonWidth, stains,
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


        private ComboWithFilter<Item> CreateItemCombo(EquipSlot slot, IReadOnlyList<Item> items)
            => new($"{_equipSlotNames[slot]}##Equip", ItemComboWidth, ItemComboWidth, items, i => i.Name)
            {
                Flags = ImGuiComboFlags.HeightLarge,
            };

        private (ComboWithFilter<Item>, ComboWithFilter<Stain>) CreateCombos(EquipSlot slot, IReadOnlyList<Item> items,
            ComboWithFilter<Stain> defaultStain)
            => (CreateItemCombo(slot, items), new ComboWithFilter<Stain>($"##{slot}Stain", defaultStain));
    }

    internal partial class Interface
    {
        private bool DrawStainSelector(ComboWithFilter<Stain> stainCombo, EquipSlot slot, StainId stainIdx)
        {
            stainCombo.PostPreview = null;
            if (_stains.TryGetValue((byte) stainIdx, out var stain))
            {
                var previewPush = PushColor(stain, ImGuiCol.FrameBg);
                stainCombo.PostPreview = () => ImGui.PopStyleColor(previewPush);
            }

            if (stainCombo.Draw(string.Empty, out var newStain) && _player != null && !newStain.RowIndex.Equals(stainIdx))
            {
                newStain.Write(_player.Address, slot);
                return true;
            }

            return false;
        }

        private bool DrawItemSelector(ComboWithFilter<Item> equipCombo, Lumina.Excel.GeneratedSheets.Item? item)
        {
            var currentName = item?.Name.ToString() ?? "Nothing";
            if (equipCombo.Draw(currentName, out var newItem, _itemComboWidth) && _player != null && newItem.Base.RowId != item?.RowId)
            {
                newItem.Write(_player.Address);
                return true;
            }

            return false;
        }

        private bool DrawEquip(EquipSlot slot, ActorArmor equip)
        {
            var (equipCombo, stainCombo) = _combos[slot];

            var ret = DrawStainSelector(stainCombo, slot, equip.Stain);
            ImGui.SameLine();
            var item = _identifier.Identify(equip.Set, new WeaponType(), equip.Variant, slot);
            ret |= DrawItemSelector(equipCombo, item);

            return ret;
        }

        private bool DrawWeapon(EquipSlot slot, ActorWeapon weapon)
        {
            var (equipCombo, stainCombo) = _combos[slot];

            var ret = DrawStainSelector(stainCombo, slot, weapon.Stain);
            ImGui.SameLine();
            var item = _identifier.Identify(weapon.Set, weapon.Type, weapon.Variant, slot);
            ret |= DrawItemSelector(equipCombo, item);

            return ret;
        }
    }

    internal partial class Interface
    {
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
                if (ImGui.ColorButton((i + 1).ToString(), ImGui.ColorConvertU32ToFloat4(custom.Color)))
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

        private Vector2 _iconSize          = Vector2.Zero;
        private Vector2 _actualIconSize    = Vector2.Zero;
        private float   _raceSelectorWidth = 0;
        private float   _inputIntSize      = 0;
        private float   _comboSelectorSize = 0;
        private float   _percentageSize    = 0;
        private float   _itemComboWidth    = 0;

        private bool InputInt(string label, ref int value, int minValue, int maxValue)
        {
            var ret = false;
            var tmp = value + 1;
            ImGui.SetNextItemWidth(_inputIntSize);
            if (ImGui.InputInt(label, ref tmp, 1) && tmp != value + 1 && tmp >= minValue && tmp <= maxValue)
            {
                value = tmp - 1;
                ret   = true;
            }

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip($"Input Range: [{minValue}, {maxValue}]");

            return ret;
        }

        private static (int, Customization.Customization) GetCurrentCustomization(LazyCustomization customization, CustomizationId id,
            CustomizationSet set)
        {
            var current = set.DataByValue(id, customization.Value[id], out var custom);
            if (current < 0)
            {
                PluginLog.Warning($"Read invalid customization value {customization.Value[id]} for {id}.");
                current = 0;
                custom  = set.Data(id, 0);
            }

            return (current, custom!.Value);
        }

        private bool DrawColorPicker(string label, string tooltip, LazyCustomization customization, CustomizationId id,
            CustomizationSet set)
        {
            var ret   = false;
            var count = set.Count(id);

            var (current, custom) = GetCurrentCustomization(customization, id, set);

            var popupName = $"Color Picker##{id}";
            if (ImGui.ColorButton($"{current + 1}##color_{id}", ImGui.ColorConvertU32ToFloat4(custom.Color), ImGuiColorEditFlags.None,
                _actualIconSize))
                ImGui.OpenPopup(popupName);

            ImGui.SameLine();

            using (var group = ImGuiRaii.NewGroup())
            {
                if (InputInt($"##text_{id}", ref current, 1, count))
                {
                    customization.Value[id] = set.Data(id, current - 1).Value;
                    ret                     = true;
                }


                ImGui.Text(label);
                if (tooltip.Any() && ImGui.IsItemHovered())
                    ImGui.SetTooltip(tooltip);
            }

            if (!DrawColorPickerPopup(popupName, set, id, out var newCustom))
                return ret;

            customization.Value[id] = newCustom.Value;
            ret                     = true;

            return ret;
        }
    }

    internal partial class Interface : IDisposable
    {
        public const     int    GPoseActorId = 201;
        private const    string PluginName   = "Glamourer";
        private readonly string _glamourerHeader;

        private readonly IReadOnlyDictionary<byte, Stain>                                       _stains;
        private readonly IReadOnlyDictionary<EquipSlot, List<Item>>                             _equip;
        private readonly ActorTable                                                             _actors;
        private readonly IObjectIdentifier                                                      _identifier;
        private readonly Dictionary<EquipSlot, (ComboWithFilter<Item>, ComboWithFilter<Stain>)> _combos;
        private readonly IPlayerWatcher                                                         _playerWatcher;
        private readonly ImGuiScene.TextureWrap?                                                _legacyTattooIcon;
        private readonly Dictionary<EquipSlot, string>                                          _equipSlotNames;

        private bool _visible = false;

        private Actor? _player;

        private static ImGuiScene.TextureWrap? GetLegacyTattooIcon()
        {
            using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("Glamourer.LegacyTattoo.raw");
            if (resource != null)
            {
                var rawImage = new byte[resource.Length];
                resource.Read(rawImage, 0, (int) resource.Length);
                return GlamourerPlugin.PluginInterface.UiBuilder.LoadImageRaw(rawImage, 192, 192, 4);
            }

            return null;
        }

        private static Dictionary<EquipSlot, string> GetEquipSlotNames()
        {
            var sheet = GlamourerPlugin.PluginInterface.Data.GetExcelSheet<Addon>();
            var ret = new Dictionary<EquipSlot, string>(12)
            {
                [EquipSlot.MainHand] = sheet.GetRow(738)?.Text.ToString() ?? "Main Hand",
                [EquipSlot.OffHand]  = sheet.GetRow(739)?.Text.ToString() ?? "Off Hand",
                [EquipSlot.Head]     = sheet.GetRow(740)?.Text.ToString() ?? "Head",
                [EquipSlot.Body]     = sheet.GetRow(741)?.Text.ToString() ?? "Body",
                [EquipSlot.Hands]    = sheet.GetRow(750)?.Text.ToString() ?? "Hands",
                [EquipSlot.Legs]     = sheet.GetRow(742)?.Text.ToString() ?? "Legs",
                [EquipSlot.Feet]     = sheet.GetRow(744)?.Text.ToString() ?? "Feet",
                [EquipSlot.Ears]     = sheet.GetRow(745)?.Text.ToString() ?? "Ears",
                [EquipSlot.Neck]     = sheet.GetRow(746)?.Text.ToString() ?? "Neck",
                [EquipSlot.Wrists]   = sheet.GetRow(747)?.Text.ToString() ?? "Wrists",
                [EquipSlot.RFinger]  = sheet.GetRow(748)?.Text.ToString() ?? "Right Ring",
                [EquipSlot.LFinger]  = sheet.GetRow(749)?.Text.ToString() ?? "Left Ring",
            };
            return ret;
        }

        public Interface()
        {
            _glamourerHeader = GlamourerPlugin.Version.Length > 0
                ? $"{PluginName} v{GlamourerPlugin.Version}###{PluginName}Main"
                : $"{PluginName}###{PluginName}Main";
            GlamourerPlugin.PluginInterface.UiBuilder.OnBuildUi      += Draw;
            GlamourerPlugin.PluginInterface.UiBuilder.OnOpenConfigUi += ToggleVisibility;

            _equipSlotNames = GetEquipSlotNames();

            _stains        = GameData.Stains(GlamourerPlugin.PluginInterface);
            _equip         = GameData.ItemsBySlot(GlamourerPlugin.PluginInterface);
            _identifier    = Penumbra.GameData.GameData.GetIdentifier(GlamourerPlugin.PluginInterface);
            _actors        = GlamourerPlugin.PluginInterface.ClientState.Actors;
            _playerWatcher = PlayerWatchFactory.Create(GlamourerPlugin.PluginInterface);

            var stainCombo = CreateDefaultStainCombo(_stains.Values.ToArray());

            _combos           = _equip.ToDictionary(kvp => kvp.Key, kvp => CreateCombos(kvp.Key, kvp.Value, stainCombo));
            _legacyTattooIcon = GetLegacyTattooIcon();
        }

        public void ToggleVisibility(object _, object _2)
            => _visible = !_visible;

        public void Dispose()
        {
            _legacyTattooIcon?.Dispose();
            _playerWatcher?.Dispose();
            GlamourerPlugin.PluginInterface.UiBuilder.OnBuildUi      -= Draw;
            GlamourerPlugin.PluginInterface.UiBuilder.OnOpenConfigUi -= ToggleVisibility;
        }

        private string _currentActorName = "";

        private SubRace _currentSubRace = SubRace.Midlander;
        private Gender  _currentGender  = Gender.Male;

        private bool DrawListSelector(string label, string tooltip, LazyCustomization customization, CustomizationId id,
            CustomizationSet set)
        {
            using var bigGroup = ImGuiRaii.NewGroup();
            var       ret      = false;
            int       current  = customization.Value[id];
            var       count    = set.Count(id);

            ImGui.SetNextItemWidth(_comboSelectorSize * ImGui.GetIO().FontGlobalScale);
            if (ImGui.BeginCombo($"##combo_{id}", $"{set.Option(id)} #{current + 1}"))
            {
                for (var i = 0; i < count; ++i)
                {
                    if (ImGui.Selectable($"{set.Option(id)} #{i + 1}##combo", i == current) && i != current)
                    {
                        customization.Value[id] = (byte) i;
                        ret                     = true;
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.SameLine();
            if (InputInt($"##text_{id}", ref current, 1, count))
            {
                customization.Value[id] = set.Data(id, current).Value;
                ret                     = true;
            }

            ImGui.SameLine();
            ImGui.Text(label);
            if (tooltip.Any() && ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);

            return ret;
        }

        private static readonly Vector4 NoColor  = new(1f, 1f, 1f, 1f);
        private static readonly Vector4 RedColor = new(0.6f, 0.3f, 0.3f, 1f);

        private bool DrawMultiSelector(LazyCustomization customization, CustomizationSet set)
        {
            using var bigGroup = ImGuiRaii.NewGroup();
            var       ret      = false;
            var       count    = set.Count(CustomizationId.FacialFeaturesTattoos);
            using (var raii = ImGuiRaii.NewGroup())
            {
                for (var i = 0; i < count; ++i)
                {
                    var enabled = customization.Value.FacialFeature(i);
                    var feature = set.FacialFeature(set.Race == Race.Hrothgar ? customization.Value.Hairstyle : customization.Value.Face, i);
                    var icon = i == count - 1
                        ? _legacyTattooIcon ?? GlamourerPlugin.Customization.GetIcon(feature.IconId)
                        : GlamourerPlugin.Customization.GetIcon(feature.IconId);
                    if (ImGui.ImageButton(icon.ImGuiHandle, _iconSize, Vector2.Zero, Vector2.One, (int) ImGui.GetStyle().FramePadding.X,
                        Vector4.Zero,
                        enabled ? NoColor : RedColor))
                    {
                        ret = true;
                        customization.Value.FacialFeature(i, !enabled);
                    }

                    if (ImGui.IsItemHovered())
                    {
                        using var tt = ImGuiRaii.NewTooltip();
                        ImGui.Image(icon.ImGuiHandle, new Vector2(icon.Width, icon.Height));
                    }

                    if (i % 4 != 3)
                        ImGui.SameLine();
                }
            }

            ImGui.SameLine();
            using var group = ImGuiRaii.NewGroup();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetTextLineHeightWithSpacing() + 3 * ImGui.GetStyle().ItemSpacing.Y / 2);
            int value = customization.Value[CustomizationId.FacialFeaturesTattoos];
            if (InputInt($"##{CustomizationId.FacialFeaturesTattoos}", ref value, 1, 256))
            {
                customization.Value[CustomizationId.FacialFeaturesTattoos] = (byte) value;
                ret                                                        = true;
            }

            ImGui.Text(set.Option(CustomizationId.FacialFeaturesTattoos));

            return ret;
        }

        private bool DrawIconPickerPopup(string label, CustomizationSet set, CustomizationId id, out Customization.Customization value)
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
                if (ImGui.ImageButton(icon.ImGuiHandle, _iconSize))
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

        private bool DrawIconSelector(string label, string tooltip, LazyCustomization customization, CustomizationId id,
            CustomizationSet set)
        {
            using var bigGroup = ImGuiRaii.NewGroup();
            var       ret      = false;
            var       count    = set.Count(id);

            var current = set.DataByValue(id, customization.Value[id], out var custom);
            if (current < 0)
            {
                PluginLog.Warning($"Read invalid customization value {customization.Value[id]} for {id}.");
                current = 0;
                custom  = set.Data(id, 0);
            }

            var popupName = $"Style Picker##{id}";
            var icon      = GlamourerPlugin.Customization.GetIcon(custom!.Value.IconId);
            if (ImGui.ImageButton(icon.ImGuiHandle, _iconSize))
                ImGui.OpenPopup(popupName);

            if (ImGui.IsItemHovered())
            {
                using var tt = ImGuiRaii.NewTooltip();
                ImGui.Image(icon.ImGuiHandle, new Vector2(icon.Width, icon.Height));
            }

            ImGui.SameLine();
            using var group = ImGuiRaii.NewGroup();
            if (InputInt($"##text_{id}", ref current, 1, count))
            {
                customization.Value[id] = set.Data(id, current).Value;
                ret                     = true;
            }

            if (DrawIconPickerPopup(popupName, set, id, out var newCustom))
            {
                customization.Value[id] = newCustom.Value;
                ret                     = true;
            }

            ImGui.Text(label);
            if (tooltip.Any() && ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);

            return ret;
        }


        private bool DrawPercentageSelector(string label, string tooltip, LazyCustomization customization, CustomizationId id,
            CustomizationSet set)
        {
            using var bigGroup = ImGuiRaii.NewGroup();
            var       ret      = false;
            int       value    = customization.Value[id];
            var       count    = set.Count(id);
            ImGui.SetNextItemWidth(_percentageSize * ImGui.GetIO().FontGlobalScale);
            if (ImGui.SliderInt($"##slider_{id}", ref value, 0, count - 1, "") && value != customization.Value[id])
            {
                customization.Value[id] = (byte) value;
                ret                     = true;
            }

            ImGui.SameLine();
            --value;
            if (InputInt($"##input_{id}", ref value, 0, count - 1))
            {
                customization.Value[id] = (byte) (value + 1);
                ret                     = true;
            }

            ImGui.SameLine();
            ImGui.Text(label);
            if (tooltip.Any() && ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);

            return ret;
        }

        private string ClanName(SubRace race, Gender gender)
        {
            if (gender == Gender.Female)
                return race switch
                {
                    SubRace.Midlander       => GlamourerPlugin.Customization.GetName(CustomName.MidlanderM),
                    SubRace.Highlander      => GlamourerPlugin.Customization.GetName(CustomName.HighlanderM),
                    SubRace.Wildwood        => GlamourerPlugin.Customization.GetName(CustomName.WildwoodM),
                    SubRace.Duskwight       => GlamourerPlugin.Customization.GetName(CustomName.DuskwightM),
                    SubRace.Plainsfolk      => GlamourerPlugin.Customization.GetName(CustomName.PlainsfolkM),
                    SubRace.Dunesfolk       => GlamourerPlugin.Customization.GetName(CustomName.DunesfolkM),
                    SubRace.SeekerOfTheSun  => GlamourerPlugin.Customization.GetName(CustomName.SeekerOfTheSunM),
                    SubRace.KeeperOfTheMoon => GlamourerPlugin.Customization.GetName(CustomName.KeeperOfTheMoonM),
                    SubRace.Seawolf         => GlamourerPlugin.Customization.GetName(CustomName.SeawolfM),
                    SubRace.Hellsguard      => GlamourerPlugin.Customization.GetName(CustomName.HellsguardM),
                    SubRace.Raen            => GlamourerPlugin.Customization.GetName(CustomName.RaenM),
                    SubRace.Xaela           => GlamourerPlugin.Customization.GetName(CustomName.XaelaM),
                    SubRace.Helion          => GlamourerPlugin.Customization.GetName(CustomName.HelionM),
                    SubRace.Lost            => GlamourerPlugin.Customization.GetName(CustomName.LostM),
                    SubRace.Rava            => GlamourerPlugin.Customization.GetName(CustomName.RavaF),
                    SubRace.Veena           => GlamourerPlugin.Customization.GetName(CustomName.VeenaF),
                    _                       => throw new ArgumentOutOfRangeException(nameof(race), race, null),
                };

            return race switch
            {
                SubRace.Midlander       => GlamourerPlugin.Customization.GetName(CustomName.MidlanderF),
                SubRace.Highlander      => GlamourerPlugin.Customization.GetName(CustomName.HighlanderF),
                SubRace.Wildwood        => GlamourerPlugin.Customization.GetName(CustomName.WildwoodF),
                SubRace.Duskwight       => GlamourerPlugin.Customization.GetName(CustomName.DuskwightF),
                SubRace.Plainsfolk      => GlamourerPlugin.Customization.GetName(CustomName.PlainsfolkF),
                SubRace.Dunesfolk       => GlamourerPlugin.Customization.GetName(CustomName.DunesfolkF),
                SubRace.SeekerOfTheSun  => GlamourerPlugin.Customization.GetName(CustomName.SeekerOfTheSunF),
                SubRace.KeeperOfTheMoon => GlamourerPlugin.Customization.GetName(CustomName.KeeperOfTheMoonF),
                SubRace.Seawolf         => GlamourerPlugin.Customization.GetName(CustomName.SeawolfF),
                SubRace.Hellsguard      => GlamourerPlugin.Customization.GetName(CustomName.HellsguardF),
                SubRace.Raen            => GlamourerPlugin.Customization.GetName(CustomName.RaenF),
                SubRace.Xaela           => GlamourerPlugin.Customization.GetName(CustomName.XaelaF),
                SubRace.Helion          => GlamourerPlugin.Customization.GetName(CustomName.HelionM),
                SubRace.Lost            => GlamourerPlugin.Customization.GetName(CustomName.LostM),
                SubRace.Rava            => GlamourerPlugin.Customization.GetName(CustomName.RavaF),
                SubRace.Veena           => GlamourerPlugin.Customization.GetName(CustomName.VeenaF),
                _                       => throw new ArgumentOutOfRangeException(nameof(race), race, null),
            };
        }

        private bool DrawRaceSelector(LazyCustomization customization)
        {
            using var group = ImGuiRaii.NewGroup();
            var       ret   = false;
            _currentSubRace = customization.Value.Clan;
            ImGui.SetNextItemWidth(_raceSelectorWidth);
            if (ImGui.BeginCombo("##subRaceCombo", ClanName(_currentSubRace, customization.Value.Gender)))
            {
                for (var i = 0; i < (int) SubRace.Veena; ++i)
                {
                    if (ImGui.Selectable(ClanName((SubRace) i + 1, customization.Value.Gender), (int) _currentSubRace == i + 1))
                    {
                        _currentSubRace =  (SubRace) i + 1;
                        ret             |= ChangeRace(customization, _currentSubRace);
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.Text(
                $"{GlamourerPlugin.Customization.GetName(CustomName.Gender)} & {GlamourerPlugin.Customization.GetName(CustomName.Clan)}");

            return ret;
        }

        private bool DrawGenderSelector(LazyCustomization customization)
        {
            var ret = false;
            ImGui.PushFont(UiBuilder.IconFont);
            var icon       = _currentGender == Gender.Male ? FontAwesomeIcon.Mars : FontAwesomeIcon.Venus;
            var restricted = false;
            if (customization.Value.Race == Race.Viera)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.25f);
                icon       = FontAwesomeIcon.VenusDouble;
                restricted = true;
            }
            else if (customization.Value.Race == Race.Hrothgar)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.25f);
                icon       = FontAwesomeIcon.MarsDouble;
                restricted = true;
            }

            if (ImGui.Button(icon.ToIconString(), _actualIconSize) && !restricted)
            {
                _currentGender = _currentGender == Gender.Male ? Gender.Female : Gender.Male;
                ret            = ChangeGender(customization, _currentGender);
            }

            if (restricted)
                ImGui.PopStyleVar();
            ImGui.PopFont();
            return ret;
        }

        private bool DrawPicker(CustomizationSet set, CustomizationId id, LazyCustomization customization)
        {
            if (!set.IsAvailable(id))
                return false;

            switch (set.Type(id))
            {
                case CharaMakeParams.MenuType.ColorPicker: return DrawColorPicker(set.OptionName[(int) id], "", customization, id, set);
                case CharaMakeParams.MenuType.ListSelector: return DrawListSelector(set.OptionName[(int) id], "", customization, id, set);
                case CharaMakeParams.MenuType.IconSelector: return DrawIconSelector(set.OptionName[(int) id], "", customization, id, set);
                case CharaMakeParams.MenuType.MultiIconSelector: return DrawMultiSelector(customization, set);
                case CharaMakeParams.MenuType.Percentage: return DrawPercentageSelector(set.OptionName[(int) id], "", customization, id, set);
            }

            return false;
        }

        private static readonly CustomizationId[] AllCustomizations = (CustomizationId[]) Enum.GetValues(typeof(CustomizationId));

        private bool DrawStuff()
        {
            var x = new LazyCustomization(_player!.Address);
            _currentSubRace = x.Value.Clan;
            _currentGender  = x.Value.Gender;
            var ret = DrawGenderSelector(x);
            ImGui.SameLine();
            ret |= DrawRaceSelector(x);

            var set = GlamourerPlugin.Customization.GetList(_currentSubRace, _currentGender);


            foreach (var id in AllCustomizations.Where(c => set.Type(c) == CharaMakeParams.MenuType.Percentage))
                ret |= DrawPicker(set, id, x);

            var odd = true;
            foreach (var id in AllCustomizations.Where((c, i) => set.Type(c) == CharaMakeParams.MenuType.IconSelector))
            {
                ret |= DrawPicker(set, id, x);
                if (odd)
                    ImGui.SameLine();
                odd = !odd;
            }

            if (!odd)
                ImGui.NewLine();

            ret |= DrawPicker(set, CustomizationId.FacialFeaturesTattoos, x);

            foreach (var id in AllCustomizations.Where(c => set.Type(c) == CharaMakeParams.MenuType.ListSelector))
                ret |= DrawPicker(set, id, x);

            odd = true;
            foreach (var id in AllCustomizations.Where(c => set.Type(c) == CharaMakeParams.MenuType.ColorPicker))
            {
                ret |= DrawPicker(set, id, x);
                if (odd)
                    ImGui.SameLine();
                odd = !odd;
            }

            if (!odd)
                ImGui.NewLine();

            var tmp = x.Value.HighlightsOn;
            if (ImGui.Checkbox(set.Option(CustomizationId.HighlightsOnFlag), ref tmp) && tmp != x.Value.HighlightsOn)
            {
                x.Value.HighlightsOn = tmp;
                ret                  = true;
            }

            var xPos = _inputIntSize + _actualIconSize.X + 3 * ImGui.GetStyle().ItemSpacing.X;
            ImGui.SameLine(xPos);
            tmp = x.Value.FacePaintReversed;
            if (ImGui.Checkbox($"{GlamourerPlugin.Customization.GetName(CustomName.Reverse)} {set.Option(CustomizationId.FacePaint)}", ref tmp)
             && tmp != x.Value.FacePaintReversed)
            {
                x.Value.FacePaintReversed = tmp;
                ret                       = true;
            }

            tmp = x.Value.SmallIris;
            if (ImGui.Checkbox($"{GlamourerPlugin.Customization.GetName(CustomName.IrisSmall)} {set.Option(CustomizationId.EyeColorL)}",
                    ref tmp)
             && tmp != x.Value.SmallIris)
            {
                x.Value.SmallIris = tmp;
                ret               = true;
            }

            if (x.Value.Race != Race.Hrothgar)
            {
                tmp = x.Value.Lipstick;
                ImGui.SameLine(xPos);
                if (ImGui.Checkbox(set.Option(CustomizationId.LipColor), ref tmp) && tmp != x.Value.Lipstick)
                {
                    x.Value.Lipstick = tmp;
                    ret              = true;
                }
            }

            return ret;
        }


        private void Draw()
        {
            ImGui.SetNextWindowSizeConstraints(Vector2.One * 450 * ImGui.GetIO().FontGlobalScale,
                Vector2.One * 5000 * ImGui.GetIO().FontGlobalScale);
            if (!_visible || !ImGui.Begin(_glamourerHeader, ref _visible))
                return;

            try
            {
                var inCombo = ImGui.BeginCombo("Actor", _currentActorName);
                var idx = 0;
                _player = null;
                foreach (var actor in _actors.Where(a => a.ObjectKind == ObjectKind.Player))
                {
                    if (_currentActorName == actor.Name)
                        _player = actor;

                    if (inCombo && ImGui.Selectable($"{actor.Name}##{idx++}"))
                        _currentActorName = actor.Name;
                }

                if (_player == null)
                {
                    _player           = _actors[0];
                    _currentActorName = _player?.Name ?? string.Empty;
                }

                if (inCombo)
                    ImGui.EndCombo();

                if (_player == _actors[0] && _actors[GPoseActorId] != null)
                    _player = _actors[GPoseActorId];
                if (_player == null || !GlamourerPlugin.PluginInterface.ClientState.Condition.Any())
                {
                    ImGui.TextColored(new Vector4(0.4f, 0.1f, 0.1f, 1f),
                        "No player character available.");
                }
                else
                {
                    var equip = new ActorEquipment(_player);
                    _iconSize          = Vector2.One * ImGui.GetTextLineHeightWithSpacing() * 2;
                    _actualIconSize    = _iconSize + 2 * ImGui.GetStyle().FramePadding;
                    _comboSelectorSize = 4 * _actualIconSize.X + 3 * ImGui.GetStyle().ItemSpacing.X;
                    _percentageSize    = _comboSelectorSize;
                    _inputIntSize      = 2 * _actualIconSize.X + ImGui.GetStyle().ItemSpacing.X;
                    _raceSelectorWidth = _inputIntSize + _percentageSize - _actualIconSize.X;
                    _itemComboWidth    = 6 * _actualIconSize.X + 4 * ImGui.GetStyle().ItemSpacing.X - ColorButtonWidth + 1;
                    var changes = false;

                    if (ImGui.CollapsingHeader("Character Equipment"))
                    {
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
                    }

                    if (ImGui.CollapsingHeader("Character Customization"))
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
