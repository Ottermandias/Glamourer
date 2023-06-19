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

        _service.ChangeGender(ref _customize, _customize.Gender is Gender.Male ? Gender.Female : Gender.Male);
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
                _service.ChangeClan(ref _customize, subRace);
        }
    }
}
