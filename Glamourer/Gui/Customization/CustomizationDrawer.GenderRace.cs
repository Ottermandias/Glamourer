using System;
using System.Linq;
using Dalamud.Interface;
using Glamourer.Customization;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData.Enums;

namespace Glamourer.Gui.Customization;

public partial class CustomizationDrawer
{
    private void DrawRaceGenderSelector()
    {
        DrawGenderSelector();
        ImGui.SameLine();
        using var group = ImRaii.Group();
        DrawRaceCombo();
        var gender = _service.AwaitedService.GetName(CustomName.Gender);
        var clan   = _service.AwaitedService.GetName(CustomName.Clan);
        ImGui.TextUnformatted($"{gender} & {clan}");
    }

    private void DrawGenderSelector()
    {
        var icon = _customize.Gender switch
        {
            Gender.Male when _customize.Race is Race.Hrothgar => FontAwesomeIcon.MarsDouble,
            Gender.Male                                       => FontAwesomeIcon.Mars,
            Gender.Female                                     => FontAwesomeIcon.Venus,
            _                                                 => FontAwesomeIcon.Question,
        };

        if (!ImGuiUtil.DrawDisabledButton(icon.ToIconString(), _framedIconSize, string.Empty, icon is not FontAwesomeIcon.Mars and not FontAwesomeIcon.Venus, true))
            return;

        Changed |= _service.ChangeGender(ref _customize, icon is FontAwesomeIcon.Mars ? Gender.Female : Gender.Male);
    }

    private void DrawRaceCombo()
    {
        ImGui.SetNextItemWidth(_raceSelectorWidth);
        using var combo = ImRaii.Combo("##subRaceCombo", _service.ClanName(_customize.Clan, _customize.Gender));
        if (!combo)
            return;

        foreach (var subRace in Enum.GetValues<SubRace>().Skip(1)) // Skip Unknown
        {
            if (ImGui.Selectable(_service.ClanName(subRace, _customize.Gender), subRace == _customize.Clan))
                Changed |= _service.ChangeClan(ref _customize, subRace);
        }
    }
}
