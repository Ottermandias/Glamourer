using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
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
        if (_withApply)
        {
            using var disabled = ImRaii.Disabled(_locked);
            if (UiHelpers.DrawCheckbox("##applyGender", "Apply gender of this design.", ChangeApply.HasFlag(CustomizeFlag.Gender),
                    out var applyGender, _locked))
                ChangeApply = applyGender ? ChangeApply | CustomizeFlag.Gender : ChangeApply & ~CustomizeFlag.Gender;
            ImGui.SameLine();
            if (UiHelpers.DrawCheckbox("##applyClan", "Apply clan of this design.", ChangeApply.HasFlag(CustomizeFlag.Clan), out var applyClan,
                    _locked))
                ChangeApply = applyClan ? ChangeApply | CustomizeFlag.Clan : ChangeApply & ~CustomizeFlag.Clan;
            ImGui.SameLine();
        }

        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Gender & Clan");
    }

    private void DrawGenderSelector()
    {
        using (var disabled = ImRaii.Disabled(_locked || _lockedRedraw))
        {
            var icon = _customize.Gender switch
            {
                Gender.Male when _customize.Race is Race.Hrothgar => FontAwesomeIcon.MarsDouble,
                Gender.Male                                       => FontAwesomeIcon.Mars,
                Gender.Female                                     => FontAwesomeIcon.Venus,
                _                                                 => FontAwesomeIcon.Question,
            };

            if (ImGuiUtil.DrawDisabledButton(icon.ToIconString(), _framedIconSize, string.Empty,
                    icon is not FontAwesomeIcon.Mars and not FontAwesomeIcon.Venus, true))
                Changed |= _service.ChangeGender(ref _customize, icon is FontAwesomeIcon.Mars ? Gender.Female : Gender.Male);
        }

        if (_lockedRedraw && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(
                "The gender can not be changed as this requires a redraw of the character, which is not supported for this actor.");
    }

    private void DrawRaceCombo()
    {
        using (var disabled = ImRaii.Disabled(_locked || _lockedRedraw))
        {
            ImGui.SetNextItemWidth(_raceSelectorWidth);
            using (var combo = ImRaii.Combo("##subRaceCombo", _service.ClanName(_customize.Clan, _customize.Gender)))
            {
                if (combo)
                    foreach (var subRace in Enum.GetValues<SubRace>().Skip(1)) // Skip Unknown
                    {
                        if (ImGui.Selectable(_service.ClanName(subRace, _customize.Gender), subRace == _customize.Clan))
                            Changed |= _service.ChangeClan(ref _customize, subRace);
                    }
            }
        }

        if (_lockedRedraw && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip("The race can not be changed as this requires a redraw of the character, which is not supported for this actor.");
    }

    private void DrawBodyType()
    {
        if (_customize.BodyType.Value == 1)
            return;

        if (!ImGuiUtil.DrawDisabledButton($"Reset Body Type {_customize.BodyType.Value} to Default",
                new Vector2(_raceSelectorWidth + _framedIconSize.X + ImGui.GetStyle().ItemSpacing.X, 0), string.Empty, _lockedRedraw))
            return;

        Changed             |= CustomizeFlag.BodyType;
        _customize.BodyType =  (CustomizeValue)1;
    }
}
