using Dalamud.Interface;
using Dalamud.Bindings.ImGui;
using ImSharp;
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
        Im.Line.Same();
        using var group = ImRaii.Group();
        DrawRaceCombo();
        if (_withApply)
        {
            using var disabled = ImRaii.Disabled(_locked);
            if (UiHelpers.DrawCheckbox("##applyGender", "Apply gender of this design.", ChangeApply.HasFlag(CustomizeFlag.Gender),
                    out var applyGender, _locked))
                ChangeApply = applyGender ? ChangeApply | CustomizeFlag.Gender : ChangeApply & ~CustomizeFlag.Gender;
            Im.Line.Same();
            if (UiHelpers.DrawCheckbox("##applyClan", "Apply clan of this design.", ChangeApply.HasFlag(CustomizeFlag.Clan), out var applyClan,
                    _locked))
                ChangeApply = applyClan ? ChangeApply | CustomizeFlag.Clan : ChangeApply & ~CustomizeFlag.Clan;
            Im.Line.Same();
        }

        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Gender & Clan");
    }

    private void DrawGenderSelector()
    {
        using (ImRaii.Disabled(_locked || _lockedRedraw))
        {
            var icon = _customize.Gender switch
            {
                Gender.Male   => FontAwesomeIcon.Mars,
                Gender.Female => FontAwesomeIcon.Venus,
                _             => FontAwesomeIcon.Question,
            };

            if (ImGuiUtil.DrawDisabledButton(icon.ToIconString(), _framedIconSize, string.Empty,
                    icon is not FontAwesomeIcon.Mars and not FontAwesomeIcon.Venus, true))
                Changed |= service.ChangeGender(ref _customize, icon is FontAwesomeIcon.Mars ? Gender.Female : Gender.Male);
        }

        if (_lockedRedraw && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(
                "The gender can not be changed as this requires a redraw of the character, which is not supported for this actor.");
    }

    private void DrawRaceCombo()
    {
        using (ImRaii.Disabled(_locked || _lockedRedraw))
        {
            ImGui.SetNextItemWidth(_raceSelectorWidth);
            using (var combo = ImRaii.Combo("##subRaceCombo", service.ClanName(_customize.Clan, _customize.Gender)))
            {
                if (combo)
                    foreach (var subRace in Enum.GetValues<SubRace>().Skip(1)) // Skip Unknown
                    {
                        if (ImGui.Selectable(service.ClanName(subRace, _customize.Gender), subRace == _customize.Clan))
                            Changed |= service.ChangeClan(ref _customize, subRace);
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

        var label = _lockedRedraw
            ? $"Body Type {_customize.BodyType.Value}"
            : $"Reset Body Type {_customize.BodyType.Value} to Default";
        if (!ImGuiUtil.DrawDisabledButton(label, new Vector2(_raceSelectorWidth + _framedIconSize.X + ImGui.GetStyle().ItemSpacing.X, 0),
                string.Empty, _lockedRedraw))
            return;

        Changed             |= CustomizeFlag.BodyType;
        _customize.BodyType =  (CustomizeValue)1;
    }
}
