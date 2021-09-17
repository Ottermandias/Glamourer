using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Logging;
using Glamourer.Customization;
using ImGuiNET;
using Penumbra.GameData.Enums;

namespace Glamourer.Gui
{
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

        private Vector2 _iconSize       = Vector2.Zero;
        private Vector2 _actualIconSize = Vector2.Zero;
        private float   _raceSelectorWidth;
        private float   _inputIntSize;
        private float   _comboSelectorSize;
        private float   _percentageSize;
        private float   _itemComboWidth;

        private bool InputInt(string label, ref int value, int minValue, int maxValue)
        {
            var ret = false;
            var tmp = value + 1;
            ImGui.SetNextItemWidth(_inputIntSize);
            if (ImGui.InputInt(label, ref tmp, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue) && tmp != value + 1 && tmp >= minValue && tmp <= maxValue)
            {
                value = tmp - 1;
                ret   = true;
            }

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip($"Input Range: [{minValue}, {maxValue}]");

            return ret;
        }

        private static (int, Customization.Customization) GetCurrentCustomization(ref CharacterCustomization customization, CustomizationId id,
            CustomizationSet set)
        {
            var current = set.DataByValue(id, customization[id], out var custom);
            if (set.IsAvailable(id) && current < 0)
            {
                PluginLog.Warning($"Read invalid customization value {customization[id]} for {id}.");
                current = 0;
                custom  = set.Data(id, 0);
            }

            return (current, custom!.Value);
        }

        private bool DrawColorPicker(string label, string tooltip, ref CharacterCustomization customization, CustomizationId id,
            CustomizationSet set)
        {
            var ret   = false;
            var count = set.Count(id);

            var (current, custom) = GetCurrentCustomization(ref customization, id, set);

            var popupName = $"Color Picker##{id}";
            if (ImGui.ColorButton($"{current + 1}##color_{id}", ImGui.ColorConvertU32ToFloat4(custom.Color), ImGuiColorEditFlags.None,
                _actualIconSize))
                ImGui.OpenPopup(popupName);

            ImGui.SameLine();

            using (var _ = ImGuiRaii.NewGroup())
            {
                if (InputInt($"##text_{id}", ref current, 1, count))
                {
                    customization[id] = set.Data(id, current).Value;
                    ret               = true;
                }


                ImGui.Text(label);
                if (tooltip.Any() && ImGui.IsItemHovered())
                    ImGui.SetTooltip(tooltip);
            }

            if (!DrawColorPickerPopup(popupName, set, id, out var newCustom))
                return ret;

            customization[id] = newCustom.Value;
            ret               = true;

            return ret;
        }

        private bool DrawListSelector(string label, string tooltip, ref CharacterCustomization customization, CustomizationId id,
            CustomizationSet set)
        {
            using var bigGroup = ImGuiRaii.NewGroup();
            var       ret      = false;
            int       current  = customization[id];
            var       count    = set.Count(id);

            ImGui.SetNextItemWidth(_comboSelectorSize * ImGui.GetIO().FontGlobalScale);
            if (ImGui.BeginCombo($"##combo_{id}", $"{set.Option(id)} #{current + 1}"))
            {
                for (var i = 0; i < count; ++i)
                {
                    if (ImGui.Selectable($"{set.Option(id)} #{i + 1}##combo", i == current) && i != current)
                    {
                        customization[id] = (byte) i;
                        ret               = true;
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.SameLine();
            if (InputInt($"##text_{id}", ref current, 1, count))
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

        private bool DrawMultiSelector(ref CharacterCustomization customization, CustomizationSet set)
        {
            using var bigGroup = ImGuiRaii.NewGroup();
            var       ret      = false;
            var       count    = set.Count(CustomizationId.FacialFeaturesTattoos);
            using (var _ = ImGuiRaii.NewGroup())
            {
                for (var i = 0; i < count; ++i)
                {
                    var enabled = customization.FacialFeature(i);
                    var feature = set.FacialFeature(set.Race == Race.Hrothgar ? customization.Hairstyle : customization.Face, i);
                    var icon = i == count - 1
                        ? _legacyTattooIcon ?? Glamourer.Customization.GetIcon(feature.IconId)
                        : Glamourer.Customization.GetIcon(feature.IconId);
                    if (ImGui.ImageButton(icon.ImGuiHandle, _iconSize, Vector2.Zero, Vector2.One, (int) ImGui.GetStyle().FramePadding.X,
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

                    if (i % 4 != 3)
                        ImGui.SameLine();
                }
            }

            ImGui.SameLine();
            using var group = ImGuiRaii.NewGroup();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetTextLineHeightWithSpacing() + 3 * ImGui.GetStyle().ItemSpacing.Y / 2);
            int value = customization[CustomizationId.FacialFeaturesTattoos];
            if (InputInt($"##{CustomizationId.FacialFeaturesTattoos}", ref value, 1, 256))
            {
                customization[CustomizationId.FacialFeaturesTattoos] = (byte) value;
                ret                                                  = true;
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
                var icon   = Glamourer.Customization.GetIcon(custom.IconId);
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

        private bool DrawIconSelector(string label, string tooltip, ref CharacterCustomization customization, CustomizationId id,
            CustomizationSet set)
        {
            using var bigGroup = ImGuiRaii.NewGroup();
            var       ret      = false;
            var       count    = set.Count(id);

            var current = set.DataByValue(id, customization[id], out var custom);
            if (current < 0)
            {
                PluginLog.Warning($"Read invalid customization value {customization[id]} for {id}.");
                current = 0;
                custom  = set.Data(id, 0);
            }

            var popupName = $"Style Picker##{id}";
            var icon      = Glamourer.Customization.GetIcon(custom!.Value.IconId);
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
                customization[id] = set.Data(id, current).Value;
                ret               = true;
            }

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


        private bool DrawPercentageSelector(string label, string tooltip, ref CharacterCustomization customization, CustomizationId id,
            CustomizationSet set)
        {
            using var bigGroup = ImGuiRaii.NewGroup();
            var       ret      = false;
            int       value    = customization[id];
            var       count    = set.Count(id);
            ImGui.SetNextItemWidth(_percentageSize * ImGui.GetIO().FontGlobalScale);
            if (ImGui.SliderInt($"##slider_{id}", ref value, 0, count - 1, "") && value != customization[id])
            {
                customization[id] = (byte) value;
                ret               = true;
            }

            ImGui.SameLine();
            --value;
            if (InputInt($"##input_{id}", ref value, 0, count - 1))
            {
                customization[id] = (byte) (value + 1);
                ret               = true;
            }

            ImGui.SameLine();
            ImGui.Text(label);
            if (tooltip.Any() && ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);

            return ret;
        }

        private bool DrawRaceSelector(ref CharacterCustomization customization)
        {
            using var group = ImGuiRaii.NewGroup();
            var       ret   = false;
            ImGui.SetNextItemWidth(_raceSelectorWidth);
            if (ImGui.BeginCombo("##subRaceCombo", ClanName(customization.Clan, customization.Gender)))
            {
                for (var i = 0; i < (int) SubRace.Veena; ++i)
                {
                    if (ImGui.Selectable(ClanName((SubRace) i + 1, customization.Gender), (int) customization.Clan == i + 1))
                    {
                        var race = (SubRace) i + 1;
                        ret |= ChangeRace(ref customization, race);
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.Text(
                $"{Glamourer.Customization.GetName(CustomName.Gender)} & {Glamourer.Customization.GetName(CustomName.Clan)}");

            return ret;
        }

        private bool DrawGenderSelector(ref CharacterCustomization customization)
        {
            var ret = false;
            ImGui.PushFont(UiBuilder.IconFont);
            var icon       = customization.Gender == Gender.Male ? FontAwesomeIcon.Mars : FontAwesomeIcon.Venus;
            var restricted = false;
            if (customization.Race == Race.Viera)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.25f);
                icon       = FontAwesomeIcon.VenusDouble;
                restricted = true;
            }
            else if (customization.Race == Race.Hrothgar)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.25f);
                icon       = FontAwesomeIcon.MarsDouble;
                restricted = true;
            }

            if (ImGui.Button(icon.ToIconString(), _actualIconSize) && !restricted)
            {
                var gender = customization.Gender == Gender.Male ? Gender.Female : Gender.Male;
                ret = ChangeGender(ref customization, gender);
            }

            if (restricted)
                ImGui.PopStyleVar();
            ImGui.PopFont();
            return ret;
        }

        private bool DrawPicker(CustomizationSet set, CustomizationId id, ref CharacterCustomization customization)
        {
            if (!set.IsAvailable(id))
                return false;

            switch (set.Type(id))
            {
                case CharaMakeParams.MenuType.ColorPicker: return DrawColorPicker(set.OptionName[(int) id], "", ref customization, id, set);
                case CharaMakeParams.MenuType.ListSelector: return DrawListSelector(set.OptionName[(int) id], "", ref customization, id, set);
                case CharaMakeParams.MenuType.IconSelector: return DrawIconSelector(set.OptionName[(int) id], "", ref customization, id, set);
                case CharaMakeParams.MenuType.MultiIconSelector: return DrawMultiSelector(ref customization, set);
                case CharaMakeParams.MenuType.Percentage:
                    return DrawPercentageSelector(set.OptionName[(int) id], "", ref customization, id, set);
            }

            return false;
        }

        private static CustomizationId[] GetCustomizationOrder()
        {
            var ret = (CustomizationId[])Enum.GetValues(typeof(CustomizationId));
            ret[(int) CustomizationId.TattooColor] = CustomizationId.EyeColorL;
            ret[(int) CustomizationId.EyeColorL] = CustomizationId.EyeColorR;
            ret[(int) CustomizationId.EyeColorR] = CustomizationId.TattooColor;
            return ret;
        }

        private static readonly CustomizationId[] AllCustomizations = GetCustomizationOrder();

        private bool DrawCustomization(ref CharacterCustomization custom)
        {
            if (!ImGui.CollapsingHeader("Character Customization"))
                return false;

            var ret = DrawGenderSelector(ref custom);
            ImGui.SameLine();
            ret |= DrawRaceSelector(ref custom);

            var set = Glamourer.Customization.GetList(custom.Clan, custom.Gender);

            foreach (var id in AllCustomizations.Where(c => set.Type(c) == CharaMakeParams.MenuType.Percentage))
                ret |= DrawPicker(set, id, ref custom);

            var odd = true;
            foreach (var id in AllCustomizations.Where((c, _) => set.Type(c) == CharaMakeParams.MenuType.IconSelector))
            {
                ret |= DrawPicker(set, id, ref custom);
                if (odd)
                    ImGui.SameLine();
                odd = !odd;
            }

            if (!odd)
                ImGui.NewLine();

            ret |= DrawPicker(set, CustomizationId.FacialFeaturesTattoos, ref custom);

            foreach (var id in AllCustomizations.Where(c => set.Type(c) == CharaMakeParams.MenuType.ListSelector))
                ret |= DrawPicker(set, id, ref custom);

            odd = true;
            foreach (var id in AllCustomizations.Where(c => set.Type(c) == CharaMakeParams.MenuType.ColorPicker))
            {
                ret |= DrawPicker(set, id, ref custom);
                if (odd)
                    ImGui.SameLine();
                odd = !odd;
            }

            if (!odd)
                ImGui.NewLine();

            var tmp = custom.HighlightsOn;
            if (ImGui.Checkbox(set.Option(CustomizationId.HighlightsOnFlag), ref tmp) && tmp != custom.HighlightsOn)
            {
                custom.HighlightsOn = tmp;
                ret                 = true;
            }

            var xPos = _inputIntSize + _actualIconSize.X + 3 * ImGui.GetStyle().ItemSpacing.X;
            ImGui.SameLine(xPos);
            tmp = custom.FacePaintReversed;
            if (ImGui.Checkbox($"{Glamourer.Customization.GetName(CustomName.Reverse)} {set.Option(CustomizationId.FacePaint)}", ref tmp)
             && tmp != custom.FacePaintReversed)
            {
                custom.FacePaintReversed = tmp;
                ret                      = true;
            }

            tmp = custom.SmallIris;
            if (ImGui.Checkbox($"{Glamourer.Customization.GetName(CustomName.IrisSmall)} {Glamourer.Customization.GetName(CustomName.IrisSize)}",
                    ref tmp)
             && tmp != custom.SmallIris)
            {
                custom.SmallIris = tmp;
                ret              = true;
            }

            if (custom.Race != Race.Hrothgar)
            {
                tmp = custom.Lipstick;
                ImGui.SameLine(xPos);
                if (ImGui.Checkbox(set.Option(CustomizationId.LipColor), ref tmp) && tmp != custom.Lipstick)
                {
                    custom.Lipstick = tmp;
                    ret             = true;
                }
            }

            return ret;
        }
    }
}
