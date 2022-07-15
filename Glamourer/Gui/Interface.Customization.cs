using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Logging;
using Glamourer.Customization;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData.Enums;
using Race = Penumbra.GameData.Enums.Race;

namespace Glamourer.Gui;

internal partial class Interface
{
    private static byte            _tempStorage;
    private static CustomizationId _tempType;

    private static bool DrawCustomization(CharacterCustomization customize, CharacterEquip equip, bool locked)
    {
        if (!ImGui.CollapsingHeader("Character Customization"))
            return false;

        var ret = DrawRaceGenderSelector(customize, equip, locked);
        var set = Glamourer.Customization.GetList(customize.Clan, customize.Gender);

        foreach (var id in set.Order[CharaMakeParams.MenuType.Percentage])
            ret |= PercentageSelector(set, id, customize, locked);

        Functions.IteratePairwise(set.Order[CharaMakeParams.MenuType.IconSelector], c => DrawIconSelector(set, c, customize, locked),
            ImGui.SameLine);

        ret |= DrawMultiIconSelector(set, customize, locked);

        foreach (var id in set.Order[CharaMakeParams.MenuType.ListSelector])
            ret |= DrawListSelector(set, id, customize, locked);

        Functions.IteratePairwise(set.Order[CharaMakeParams.MenuType.ColorPicker], c => DrawColorPicker(set, c, customize, locked),
            ImGui.SameLine);

        ret |= Checkbox(set.Option(CustomizationId.HighlightsOnFlag), customize.HighlightsOn, b => customize.HighlightsOn = b, locked);
        var xPos = _inputIntSize + _framedIconSize.X + 3 * ImGui.GetStyle().ItemSpacing.X;
        ImGui.SameLine(xPos);
        ret |= Checkbox($"{Glamourer.Customization.GetName(CustomName.Reverse)} {set.Option(CustomizationId.FacePaint)}",
            customize.FacePaintReversed, b => customize.FacePaintReversed = b, locked);
        ret |= Checkbox($"{Glamourer.Customization.GetName(CustomName.IrisSmall)} {Glamourer.Customization.GetName(CustomName.IrisSize)}",
            customize.SmallIris, b => customize.SmallIris = b, locked);

        if (customize.Race != Race.Hrothgar)
        {
            ImGui.SameLine(xPos);
            ret |= Checkbox(set.Option(CustomizationId.LipColor), customize.Lipstick, b => customize.Lipstick = b, locked);
        }

        return ret;
    }

    private static bool DrawRaceGenderSelector(CharacterCustomization customize, CharacterEquip equip, bool locked)
    {
        var ret = DrawGenderSelector(customize, equip, locked);
        ImGui.SameLine();
        using var group = ImRaii.Group();
        ret |= DrawRaceCombo(customize, equip, locked);
        var gender = Glamourer.Customization.GetName(CustomName.Gender);
        var clan   = Glamourer.Customization.GetName(CustomName.Clan);
        ImGui.TextUnformatted($"{gender} & {clan}");
        return ret;
    }

    private static bool DrawGenderSelector(CharacterCustomization customize, CharacterEquip equip, bool locked)
    {
        using var font       = ImRaii.PushFont(UiBuilder.IconFont);
        var       icon       = customize.Gender == Gender.Male ? FontAwesomeIcon.Mars : FontAwesomeIcon.Venus;
        var       restricted = customize.Race == Race.Hrothgar;
        if (restricted)
            icon = FontAwesomeIcon.MarsDouble;

        if (!ImGuiUtil.DrawDisabledButton(icon.ToIconString(), _framedIconSize, string.Empty, restricted || locked, true))
            return false;

        var gender = customize.Gender == Gender.Male ? Gender.Female : Gender.Male;
        return customize.ChangeGender(gender, locked ? CharacterEquip.Null : equip);
    }

    private static bool DrawRaceCombo(CharacterCustomization customize, CharacterEquip equip, bool locked)
    {
        using var alpha = ImRaii.PushStyle(ImGuiStyleVar.Alpha, 0.5f, locked);
        ImGui.SetNextItemWidth(_raceSelectorWidth);
        using var combo = ImRaii.Combo("##subRaceCombo", customize.ClanName());
        if (!combo)
            return false;

        if (locked)
            ImGui.CloseCurrentPopup();

        var ret = false;
        foreach (var subRace in Enum.GetValues<SubRace>().Skip(1)) // Skip Unknown
        {
            if (ImGui.Selectable(CustomizeExtensions.ClanName(subRace, customize.Gender), subRace == customize.Clan))
                ret |= customize.ChangeRace(subRace, equip);
        }

        return ret;
    }

    private static bool Checkbox(string label, bool current, Action<bool> setter, bool locked)
    {
        var       tmp   = current;
        var       ret   = false;
        using var alpha = ImRaii.PushStyle(ImGuiStyleVar.Alpha, 0.5f, locked);
        if (ImGui.Checkbox($"##{label}", ref tmp) && tmp == current && !locked)
        {
            setter(tmp);
            ret = true;
        }

        alpha.Pop();

        ImGui.SameLine();
        ImGui.TextUnformatted(label);

        return ret;
    }

    private static bool PercentageSelector(CustomizationSet set, CustomizationId id, CharacterCustomization customization, bool locked)
    {
        using var bigGroup = ImRaii.Group();
        using var _        = ImRaii.PushId((int)id);
        int       value    = id == _tempType ? _tempStorage : customization[id];
        var       count    = set.Count(id);
        ImGui.SetNextItemWidth(_comboSelectorSize);

        var (min, max) = locked ? (value, value) : (0, count - 1);
        using var alpha = ImRaii.PushStyle(ImGuiStyleVar.Alpha, 0.5f, locked);
        if (ImGui.SliderInt("##slider", ref value, min, max, string.Empty, ImGuiSliderFlags.AlwaysClamp) && !locked)
        {
            _tempStorage = (byte)value;
            _tempType    = id;
        }

        var ret = ImGui.IsItemDeactivatedAfterEdit();

        ImGui.SameLine();
        ret |= InputInt("##input", id, --value, min, max, locked);

        alpha.Pop();

        ImGui.SameLine();
        ImGui.TextUnformatted(set.OptionName[(int)id]);

        if (ret)
            customization[id] = _tempStorage;

        return ret;
    }

    private static bool InputInt(string label, CustomizationId id, int startValue, int minValue, int maxValue, bool locked)
    {
        var tmp = startValue + 1;
        ImGui.SetNextItemWidth(_inputIntSize);
        if (ImGui.InputInt(label, ref tmp, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue)
         && !locked
         && tmp != startValue + 1
         && tmp >= minValue
         && tmp <= maxValue)
        {
            _tempType    = id;
            _tempStorage = (byte)(tmp - 1);
        }

        var ret = ImGui.IsItemDeactivatedAfterEdit() && !locked;
        if (!locked)
            ImGuiUtil.HoverTooltip($"Input Range: [{minValue}, {maxValue}]");
        return ret;
    }

    private static bool DrawIconSelector(CustomizationSet set, CustomizationId id, CharacterCustomization customize, bool locked)
    {
        const string popupName = "Style Picker";

        using var bigGroup = ImRaii.Group();
        using var _        = ImRaii.PushId((int)id);
        var       count    = set.Count(id);
        var       label    = set.Option(id);

        var current = set.DataByValue(id, _tempType == id ? _tempStorage : customize[id], out var custom);
        if (current < 0)
        {
            label   = $"{label} (Custom #{customize[id]})";
            current = 0;
            custom  = set.Data(id, 0);
        }

        var       icon  = Glamourer.Customization.GetIcon(custom!.Value.IconId);
        using var alpha = ImRaii.PushStyle(ImGuiStyleVar.Alpha, 0.5f, locked);
        if (ImGui.ImageButton(icon.ImGuiHandle, _iconSize) && !locked)
            ImGui.OpenPopup(popupName);

        ImGuiUtil.HoverIconTooltip(icon, _iconSize);

        ImGui.SameLine();
        using var group = ImRaii.Group();
        var (min, max) = locked ? (current, current) : (1, count);
        var ret = InputInt("##text", id, current, min, max, locked);
        if (ret)
            customize[id] = set.Data(id, _tempStorage).Value;

        ImGui.TextUnformatted($"{label} ({custom.Value.Value})");

        ret |= DrawIconPickerPopup(popupName, set, id, customize);

        return ret;
    }

    private static bool DrawIconPickerPopup(string label, CustomizationSet set, CustomizationId id, CharacterCustomization customize)
    {
        using var popup = ImRaii.Popup(label, ImGuiWindowFlags.AlwaysAutoResize);
        if (!popup)
            return false;

        var ret   = false;
        var count = set.Count(id);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
            .Push(ImGuiStyleVar.FrameRounding, 0);
        for (var i = 0; i < count; ++i)
        {
            var       custom = set.Data(id, i);
            var       icon   = Glamourer.Customization.GetIcon(custom.IconId);
            using var group  = ImRaii.Group();
            if (ImGui.ImageButton(icon.ImGuiHandle, _iconSize))
            {
                customize[id] = custom.Value;
                ret           = true;
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

        return ret;
    }

    private static bool DrawColorPicker(CustomizationSet set, CustomizationId id, CharacterCustomization customize, bool locked)
    {
        const string popupName = "Color Picker";
        using var    _         = ImRaii.PushId((int)id);
        var          ret       = false;
        var          count     = set.Count(id);
        var          label     = set.Option(id);
        var (current, custom) = GetCurrentCustomization(set, id, customize);

        using var alpha = ImRaii.PushStyle(ImGuiStyleVar.Alpha, 0.5f, locked);
        if (ImGui.ColorButton($"{current + 1}##color", ImGui.ColorConvertU32ToFloat4(custom.Color), ImGuiColorEditFlags.None, _framedIconSize)
         && !locked)
            ImGui.OpenPopup(popupName);

        ImGui.SameLine();

        using (var group = ImRaii.Group())
        {
            var (min, max) = locked ? (current, current) : (1, count);
            if (InputInt("##text", id, current, min, max, locked))
            {
                customize[id] = set.Data(id, current).Value;
                ret           = true;
            }

            ImGui.TextUnformatted(label);
        }

        return ret | DrawColorPickerPopup(popupName, set, id, customize);
    }

    private static (int, Customization.Customization) GetCurrentCustomization(CustomizationSet set, CustomizationId id,
        CharacterCustomization customize)
    {
        var current = set.DataByValue(id, customize[id], out var custom);
        if (set.IsAvailable(id) && current < 0)
        {
            PluginLog.Warning($"Read invalid customization value {customize[id]} for {id}.");
            current = 0;
            custom  = set.Data(id, 0);
        }

        return (current, custom!.Value);
    }

    private static bool DrawColorPickerPopup(string label, CustomizationSet set, CustomizationId id, CharacterCustomization customize)
    {
        using var popup = ImRaii.Popup(label, ImGuiWindowFlags.AlwaysAutoResize);
        if (!popup)
            return false;

        var ret   = false;
        var count = set.Count(id);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
            .Push(ImGuiStyleVar.FrameRounding, 0);
        for (var i = 0; i < count; ++i)
        {
            var custom = set.Data(id, i);
            if (ImGui.ColorButton((i + 1).ToString(), ImGui.ColorConvertU32ToFloat4(custom.Color)))
            {
                customize[id] = custom.Value;
                ret           = true;
                ImGui.CloseCurrentPopup();
            }

            if (i % 8 != 7)
                ImGui.SameLine();
        }

        return ret;
    }

    private static bool DrawMultiIconSelector(CustomizationSet set, CharacterCustomization customize, bool locked)
    {
        using var bigGroup = ImRaii.Group();
        using var _        = ImRaii.PushId((int)CustomizationId.FacialFeaturesTattoos);
        using var alpha    = ImRaii.PushStyle(ImGuiStyleVar.Alpha, 0.5f, locked);
        var       ret      = DrawMultiIcons(set, customize, locked);
        ImGui.SameLine();
        using var group = ImRaii.Group();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetTextLineHeightWithSpacing() + 3 * ImGui.GetStyle().ItemSpacing.Y / 2);
        int value = customize[CustomizationId.FacialFeaturesTattoos];
        var (min, max) = locked ? (value, value) : (1, 256);
        if (InputInt(string.Empty, CustomizationId.FacialFeaturesTattoos, value, min, max, locked))
        {
            customize[CustomizationId.FacialFeaturesTattoos] = (byte)value;
            ret                                              = true;
        }

        ImGui.TextUnformatted(set.Option(CustomizationId.FacialFeaturesTattoos));

        return ret;
    }

    private static bool DrawMultiIcons(CustomizationSet set, CharacterCustomization customize, bool locked)
    {
        using var _    = ImRaii.Group();
        var       face = customize.Face;
        if (set.Faces.Count < face)
            face = 1;

        var ret   = false;
        var count = set.Count(CustomizationId.FacialFeaturesTattoos);
        for (var i = 0; i < count; ++i)
        {
            var enabled = customize.FacialFeature(i);
            var feature = set.FacialFeature(face, i);
            var icon = i == count - 1
                ? LegacyTattoo ?? Glamourer.Customization.GetIcon(feature.IconId)
                : Glamourer.Customization.GetIcon(feature.IconId);
            if (ImGui.ImageButton(icon.ImGuiHandle, _iconSize, Vector2.Zero, Vector2.One, (int)ImGui.GetStyle().FramePadding.X,
                    Vector4.Zero, enabled ? Vector4.One : RedTint)
             && !locked)
            {
                customize.FacialFeature(i, !enabled);
                ret = true;
            }

            using var alpha = ImRaii.PushStyle(ImGuiStyleVar.Alpha, 1f, !locked);
            ImGuiUtil.HoverIconTooltip(icon, _iconSize);

            if (i % 4 != 3)
                ImGui.SameLine();
        }

        return ret;
    }

    private static bool DrawListSelector(CustomizationSet set, CustomizationId id, CharacterCustomization customize, bool locked)
    {
        using var _        = ImRaii.PushId((int)id);
        using var bigGroup = ImRaii.Group();
        var       ret      = false;
        int       current  = customize[id];
        var       count    = set.Count(id);

        ImGui.SetNextItemWidth(_comboSelectorSize * ImGui.GetIO().FontGlobalScale);
        using var alpha = ImRaii.PushStyle(ImGuiStyleVar.Alpha, 0.5f, locked);
        using (var combo = ImRaii.Combo("##combo", $"{set.Option(id)} #{current + 1}"))
        {
            if (combo)
                for (var i = 0; i < count; ++i)
                {
                    if (!ImGui.Selectable($"{set.Option(id)} #{i + 1}##combo", i == current) || i == current || locked)
                        continue;

                    customize[id] = (byte)i;
                    ret           = true;
                }
        }

        ImGui.SameLine();
        var (min, max) = locked ? (current, current) : (1, count);
        if (InputInt("##text", id, current, min, max, locked))
        {
            customize[id] = (byte)current;
            ret           = true;
        }

        ImGui.SameLine();
        alpha.Pop();
        ImGui.TextUnformatted(set.Option(id));

        return ret;
    }
}
