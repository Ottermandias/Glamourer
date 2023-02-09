using System;
using System.Linq;
using Dalamud.Interface;
using Glamourer.Customization;
using Glamourer.Util;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Customization;

public partial class CustomizationDrawer
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
        using var font = ImRaii.PushFont(UiBuilder.IconFont);
        var icon = _customize.Gender switch
        {
            Gender.Male when _customize.Race is Race.Hrothgar => FontAwesomeIcon.MarsDouble,
            Gender.Male                                       => FontAwesomeIcon.Mars,
            Gender.Female                                     => FontAwesomeIcon.Venus,

            _ => throw new Exception($"Gender value {_customize.Gender} is not a valid gender for a design."),
        };

        if (!ImGuiUtil.DrawDisabledButton(icon.ToIconString(), _framedIconSize, string.Empty, icon == FontAwesomeIcon.MarsDouble, true))
            return;

        Changed |= _customize.ChangeGender(CharacterEquip.Null, _customize.Gender is Gender.Male ? Gender.Female : Gender.Male);
    }

    private void DrawRaceCombo()
    {
        ImGui.SetNextItemWidth(_raceSelectorWidth);
        using var combo = ImRaii.Combo("##subRaceCombo", _customize.ClanName());
        if (!combo)
            return;

        foreach (var subRace in Enum.GetValues<SubRace>().Skip(1)) // Skip Unknown
        {
            if (ImGui.Selectable(CustomizeExtensions.ClanName(subRace, _customize.Gender), subRace == _customize.Clan))
                Changed |= _customize.ChangeRace(CharacterEquip.Null, subRace);
        }
    }
}
