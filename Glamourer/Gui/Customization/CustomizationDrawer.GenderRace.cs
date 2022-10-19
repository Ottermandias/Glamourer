using System;
using System.Linq;
using Dalamud.Interface;
using Glamourer.Customization;
using Glamourer.Util;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using Penumbra.Api.Enums;
using Penumbra.GameData.Enums;

namespace Glamourer.Gui.Customization;

internal partial class CustomizationDrawer
{
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
            Glamourer.Penumbra.RedrawObject(actor.Character, RedrawType.Redraw);
    }

    private void DrawRaceCombo()
    {
        ImGui.SetNextItemWidth(_raceSelectorWidth);
        using var combo = ImRaii.Combo("##subRaceCombo", _customize.ClanName());
        if (!combo)
            return;

        foreach (var subRace in Enum.GetValues<SubRace>().Skip(1)) // Skip Unknown
        {
            if (!ImGui.Selectable(CustomizeExtensions.ClanName(subRace, _customize.Gender), subRace == _customize.Clan)
             || !_customize.ChangeRace(_equip, subRace))
                continue;

            foreach (var actor in _actors.Where(a => a && a.DrawObject))
                Glamourer.Penumbra.RedrawObject(actor.Character, RedrawType.Redraw);
        }
    }
}
