using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Logging;
using Glamourer.Customization;
using Glamourer.Interop;
using Glamourer.Util;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Race = Penumbra.GameData.Enums.Race;

namespace Glamourer.Gui;

internal partial class Interface
{
    private class CustomizationDrawer
    {
        private Customize                  _customize;
        private CharacterEquip             _equip;
        private IReadOnlyCollection<Actor> _actors = Array.Empty<Actor>();
        private CustomizationSet           _set    = null!;

        public static void Draw(Customize customize, CharacterEquip equip, IReadOnlyCollection<Actor> actors, bool locked)
        {
            var d = new CustomizationDrawer()
            {
                _customize = customize,
                _equip     = equip,
                _actors    = actors,
            };
            

            if (!ImGui.CollapsingHeader("Character Customization"))
                return;

            using var disabled = ImRaii.Disabled(locked);

            d.DrawRaceGenderSelector();

            d._set = Glamourer.Customization.GetList(customize.Clan, customize.Gender);

            foreach (var id in d._set.Order[CharaMakeParams.MenuType.Percentage])
                d.PercentageSelector(id);

            Functions.IteratePairwise(d._set.Order[CharaMakeParams.MenuType.IconSelector], d.DrawIconSelector, ImGui.SameLine);

            d.DrawMultiIconSelector();

            foreach (var id in d._set.Order[CharaMakeParams.MenuType.ListSelector])
                d.DrawListSelector(id);

            Functions.IteratePairwise(d._set.Order[CharaMakeParams.MenuType.ColorPicker], d.DrawColorPicker, ImGui.SameLine);

            d.Checkbox(d._set.Option(CustomizationId.HighlightsOnFlag), customize.HighlightsOn, b => customize.HighlightsOn = b);
            var xPos = _inputIntSize + _framedIconSize.X + 3 * ImGui.GetStyle().ItemSpacing.X;
            ImGui.SameLine(xPos);
            d.Checkbox($"{Glamourer.Customization.GetName(CustomName.Reverse)} {d._set.Option(CustomizationId.FacePaint)}",
                customize.FacePaintReversed, b => customize.FacePaintReversed = b);
            d.Checkbox($"{Glamourer.Customization.GetName(CustomName.IrisSmall)} {Glamourer.Customization.GetName(CustomName.IrisSize)}",
                customize.SmallIris, b => customize.SmallIris = b);

            if (customize.Race != Race.Hrothgar)
            {
                ImGui.SameLine(xPos);
                d.Checkbox(d._set.Option(CustomizationId.LipColor), customize.Lipstick, b => customize.Lipstick = b);
            }
        }

        private void DrawRaceGenderSelector()
        {
            DrawGenderSelector();
            ImGui.SameLine();
            using var group = ImRaii.Group();
            DrawRaceCombo();
            var gender = Glamourer.Customization.GetName(CustomName.Gender);
            var clan   = Glamourer.Customization.GetName(CustomName.Clan);
            ImGui.TextUnformatted($"{gender} & {clan}");
        }

        private void DrawGenderSelector()
        {
            using var font       = ImRaii.PushFont(UiBuilder.IconFont);
            var       icon       = _customize.Gender == Gender.Male ? FontAwesomeIcon.Mars : FontAwesomeIcon.Venus;
            var       restricted = _customize.Race == Race.Hrothgar;
            if (restricted)
                icon = FontAwesomeIcon.MarsDouble;

            if (!ImGuiUtil.DrawDisabledButton(icon.ToIconString(), _framedIconSize, string.Empty, restricted, true))
                return;

            var gender = _customize.Gender == Gender.Male ? Gender.Female : Gender.Male;
            if (!_customize.ChangeGender(_equip, gender))
                return;

            foreach (var actor in _actors.Where(a => a))
                Glamourer.Penumbra.RedrawObject(actor.Character, RedrawType.Redraw, false);
        }

        private void DrawRaceCombo()
        {
            ImGui.SetNextItemWidth(_raceSelectorWidth);
            using var combo = ImRaii.Combo("##subRaceCombo", _customize.ClanName());
            if (!combo)
                return;

            foreach (var subRace in Enum.GetValues<SubRace>().Skip(1)) // Skip Unknown
            {
                if (ImGui.Selectable(CustomizeExtensions.ClanName(subRace, _customize.Gender), subRace == _customize.Clan)
                 && _customize.ChangeRace(_equip, subRace))
                    foreach (var actor in _actors.Where(a => a && a.DrawObject))
                        Glamourer.Penumbra.RedrawObject(actor.Character, RedrawType.Redraw, false);
            }
        }

        private void Checkbox(string label, bool current, Action<bool> setter)
        {
            var tmp = current;
            if (ImGui.Checkbox($"##{label}", ref tmp) && tmp != current)
            {
                setter(tmp);
                foreach (var actor in _actors.Where(a => a && a.DrawObject))
                    Glamourer.RedrawManager.UpdateCustomize(actor.DrawObject, _customize);
            }

            ImGui.SameLine();
            ImGui.TextUnformatted(label);
        }

        private void PercentageSelector(CustomizationId id)
        {
            using var bigGroup = ImRaii.Group();
            using var _        = ImRaii.PushId((int)id);
            int       value    = _customize[id];
            var       count    = _set.Count(id);
            ImGui.SetNextItemWidth(_comboSelectorSize);

            void OnChange(int v)
            {
                _customize[id] = (byte)v;
                foreach (var actor in _actors.Where(a => a && a.DrawObject))
                    Glamourer.RedrawManager.UpdateCustomize(actor.DrawObject, _customize);
            }

            if (ImGui.SliderInt("##slider", ref value, 0, count - 1, "%i", ImGuiSliderFlags.AlwaysClamp))
                OnChange(value);

            ImGui.SameLine();
            InputInt("##input", --value, 0, count - 1, OnChange);

            ImGui.SameLine();
            ImGui.TextUnformatted(_set.OptionName[(int)id]);
        }

        private static void InputInt(string label, int startValue, int minValue, int maxValue, Action<int> setter)
        {
            var tmp = startValue + 1;
            ImGui.SetNextItemWidth(_inputIntSize);
            if (ImGui.InputInt(label, ref tmp, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue)
             && tmp != startValue + 1
             && tmp >= minValue
             && tmp <= maxValue)
                setter(tmp);

            ImGuiUtil.HoverTooltip($"Input Range: [{minValue}, {maxValue}]");
        }

        private void DrawIconSelector(CustomizationId id)
        {
            const string popupName = "Style Picker";

            using var bigGroup = ImRaii.Group();
            using var _        = ImRaii.PushId((int)id);
            var       count    = _set.Count(id, _customize.Face);
            var       label    = _set.Option(id);

            var current = _set.DataByValue(id, _customize[id], out var custom);
            if (current < 0)
            {
                label   = $"{label} (Custom #{_customize[id]})";
                current = 0;
                custom  = _set.Data(id, 0);
            }

            var icon = Glamourer.Customization.GetIcon(custom!.Value.IconId);
            if (ImGui.ImageButton(icon.ImGuiHandle, _iconSize))
                ImGui.OpenPopup(popupName);

            ImGuiUtil.HoverIconTooltip(icon, _iconSize);

            void OnChange(int v)
            {
                var value = _set.Data(id, v - 1).Value;
                // Hrothgar hack
                if (_set.Race == Race.Hrothgar && id == CustomizationId.Face)
                    value += 4;

                if (_customize[id] == value)
                    return;

                _customize[id] = value;
                foreach (var actor in _actors.Where(a => a && a.DrawObject))
                    Glamourer.RedrawManager.UpdateCustomize(actor.DrawObject, _customize);
            }

            ImGui.SameLine();
            using var group = ImRaii.Group();
            InputInt("##text", current, 1, count, OnChange);

            ImGui.TextUnformatted($"{label} ({custom.Value.Value})");

            DrawIconPickerPopup(popupName, id, OnChange);
        }

        private void DrawIconPickerPopup(string label, CustomizationId id, Action<int> setter)
        {
            using var popup = ImRaii.Popup(label, ImGuiWindowFlags.AlwaysAutoResize);
            if (!popup)
                return;

            var count = _set.Count(id, _customize.Face);
            using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
                .Push(ImGuiStyleVar.FrameRounding, 0);
            for (var i = 0; i < count; ++i)
            {
                var       custom = _set.Data(id, i, _customize.Face);
                var       icon   = Glamourer.Customization.GetIcon(custom.IconId);
                using var group  = ImRaii.Group();
                if (ImGui.ImageButton(icon.ImGuiHandle, _iconSize))
                {
                    setter(custom.Value);
                    ImGui.CloseCurrentPopup();
                }

                ImGuiUtil.HoverIconTooltip(icon, _iconSize);

                var text      = custom.Value.ToString();
                var textWidth = ImGui.CalcTextSize(text).X;
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (_iconSize.X - textWidth + 2 * ImGui.GetStyle().FramePadding.X) / 2);
                ImGui.TextUnformatted(text);
                group.Dispose();

                if (i % 8 != 7)
                    ImGui.SameLine();
            }
        }

        private void DrawColorPicker(CustomizationId id)
        {
            const string popupName = "Color Picker";
            using var    _         = ImRaii.PushId((int)id);
            var          count     = _set.Count(id);
            var          label     = _set.Option(id);
            var (current, custom) = GetCurrentCustomization(id);

            if (ImGui.ColorButton($"{current + 1}##color", ImGui.ColorConvertU32ToFloat4(custom.Color), ImGuiColorEditFlags.None,
                    _framedIconSize))
                ImGui.OpenPopup(popupName);

            ImGui.SameLine();

            void OnChange(int v)
            {
                _customize[id] = _set.Data(id, v).Value;
                foreach (var actor in _actors.Where(a => a && a.DrawObject))
                    Glamourer.RedrawManager.UpdateCustomize(actor.DrawObject, _customize);
            }

            using (var group = ImRaii.Group())
            {
                InputInt("##text", current, 1, count, OnChange);
                ImGui.TextUnformatted(label);
            }

            DrawColorPickerPopup(popupName, id, OnChange);
        }

        private (int, Customization.Customization) GetCurrentCustomization(CustomizationId id)
        {
            var current = _set.DataByValue(id, _customize[id], out var custom);
            if (_set.IsAvailable(id) && current < 0)
            {
                PluginLog.Warning($"Read invalid customization value {_customize[id]} for {id}.");
                current = 0;
                custom  = _set.Data(id, 0);
            }

            return (current, custom!.Value);
        }

        private void DrawColorPickerPopup(string label, CustomizationId id, Action<int> setter)
        {
            using var popup = ImRaii.Popup(label, ImGuiWindowFlags.AlwaysAutoResize);
            if (!popup)
                return;

            var count = _set.Count(id);
            using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
                .Push(ImGuiStyleVar.FrameRounding, 0);
            for (var i = 0; i < count; ++i)
            {
                var custom = _set.Data(id, i);
                if (ImGui.ColorButton((i + 1).ToString(), ImGui.ColorConvertU32ToFloat4(custom.Color)))
                {
                    setter(custom.Value);
                    ImGui.CloseCurrentPopup();
                }

                if (i % 8 != 7)
                    ImGui.SameLine();
            }
        }

        private void DrawMultiIconSelector()
        {
            using var bigGroup = ImRaii.Group();
            using var _        = ImRaii.PushId((int)CustomizationId.FacialFeaturesTattoos);

            void OnChange(int v)
            {
                _customize[CustomizationId.FacialFeaturesTattoos] = (byte)v;
                foreach (var actor in _actors.Where(a => a && a.DrawObject))
                    Glamourer.RedrawManager.UpdateCustomize(actor.DrawObject, _customize);
            }

            DrawMultiIcons();
            ImGui.SameLine();
            int       value = _customize[CustomizationId.FacialFeaturesTattoos];
            using var group = ImRaii.Group();
            ImGui.Dummy(new Vector2(0, ImGui.GetTextLineHeightWithSpacing() + ImGui.GetStyle().ItemSpacing.Y / 2));
            InputInt(string.Empty, --value, 0, 255, OnChange);

            ImGui.TextUnformatted(_set.Option(CustomizationId.FacialFeaturesTattoos));
        }

        private void DrawMultiIcons()
        {
            using var _    = ImRaii.Group();
            var       face = _customize.Face;

            var ret   = false;
            var count = _set.Count(CustomizationId.FacialFeaturesTattoos);
            for (var i = 0; i < count; ++i)
            {
                var enabled = _customize.FacialFeatures[i];
                var feature = _set.FacialFeature(face, i);
                var icon = i == count - 1
                    ? LegacyTattoo ?? Glamourer.Customization.GetIcon(feature.IconId)
                    : Glamourer.Customization.GetIcon(feature.IconId);
                if (ImGui.ImageButton(icon.ImGuiHandle, _iconSize, Vector2.Zero, Vector2.One, (int)ImGui.GetStyle().FramePadding.X,
                        Vector4.Zero, enabled ? Vector4.One : RedTint))
                {
                    _customize.FacialFeatures.Set(i, !enabled);
                    foreach (var actor in _actors.Where(a => a && a.DrawObject))
                        Glamourer.RedrawManager.UpdateCustomize(actor.DrawObject, _customize);
                }

                ImGuiUtil.HoverIconTooltip(icon, _iconSize);
                if (i % 4 != 3)
                    ImGui.SameLine();
            }
        }

        private void DrawListSelector(CustomizationId id)
        {
            using var _        = ImRaii.PushId((int)id);
            using var bigGroup = ImRaii.Group();
            int       current  = _customize[id];
            var       count    = _set.Count(id);

            void OnChange(int v)
            {
                _customize[id] = (byte)v;
                foreach (var actor in _actors.Where(a => a && a.DrawObject))
                    Glamourer.RedrawManager.UpdateCustomize(actor.DrawObject, _customize);
            }

            ImGui.SetNextItemWidth(_comboSelectorSize * ImGui.GetIO().FontGlobalScale);
            using (var combo = ImRaii.Combo("##combo", $"{_set.Option(id)} #{current + 1}"))
            {
                if (combo)
                    for (var i = 0; i < count; ++i)
                    {
                        if (!ImGui.Selectable($"{_set.Option(id)} #{i + 1}##combo", i == current) || i == current)
                            continue;

                        OnChange(i);
                    }
            }

            ImGui.SameLine();
            InputInt("##text", current, 1, count, OnChange);

            ImGui.SameLine();
            ImGui.TextUnformatted(_set.Option(id));
        }
    }
}
