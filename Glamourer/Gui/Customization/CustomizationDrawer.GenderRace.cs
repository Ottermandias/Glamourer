using Dalamud.Interface;
using ImSharp;
using Luna;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Customization;

public partial class CustomizationDrawer
{
    private static readonly AwesomeIcon Male    = FontAwesomeIcon.Mars;
    private static readonly AwesomeIcon Female  = FontAwesomeIcon.Venus;
    private static readonly AwesomeIcon Unknown = FontAwesomeIcon.Question;

    private void DrawRaceGenderSelector()
    {
        DrawGenderSelector();
        Im.Line.Same();
        using var group = Im.Group();
        DrawRaceCombo();
        if (_withApply)
        {
            using var disabled = Im.Disabled(_locked);
            if (UiHelpers.DrawCheckbox("##applyGender"u8, "Apply gender of this design."u8, ChangeApply.HasFlag(CustomizeFlag.Gender),
                    out var applyGender, _locked))
                ChangeApply = applyGender ? ChangeApply | CustomizeFlag.Gender : ChangeApply & ~CustomizeFlag.Gender;
            Im.Line.Same();
            if (UiHelpers.DrawCheckbox("##applyClan"u8, "Apply clan of this design."u8, ChangeApply.HasFlag(CustomizeFlag.Clan), out var applyClan,
                    _locked))
                ChangeApply = applyClan ? ChangeApply | CustomizeFlag.Clan : ChangeApply & ~CustomizeFlag.Clan;
            Im.Line.Same();
        }

        ImEx.TextFrameAligned("Gender & Clan"u8);
    }

    private void DrawGenderSelector()
    {
        using (Im.Disabled(_locked || _lockedRedraw))
        {
            var icon = _customize.Gender switch
            {
                Gender.Male   => Male,
                Gender.Female => Female,
                _             => Unknown,
            };

            if (ImEx.Icon.Button(icon, StringU8.Empty, icon == Unknown, _framedIconSize))
                Changed |= service.ChangeGender(ref _customize, icon == Male ? Gender.Female : Gender.Male);
        }

        if (_lockedRedraw)
            Im.Tooltip.OnHover(HoveredFlags.AllowWhenDisabled,
                "The gender can not be changed as this requires a redraw of the character, which is not supported for this actor."u8);
    }

    private void DrawRaceCombo()
    {
        using (Im.Disabled(_locked || _lockedRedraw))
        {
            Im.Item.SetNextWidth(_raceSelectorWidth);
            using (var combo = Im.Combo.Begin("##subRaceCombo"u8, service.ClanName(_customize.Clan, _customize.Gender)))
            {
                if (combo)
                    foreach (var subRace in SubRace.Values.Skip(1)) // Skip Unknown
                    {
                        if (Im.Selectable(service.ClanName(subRace, _customize.Gender), subRace == _customize.Clan))
                            Changed |= service.ChangeClan(ref _customize, subRace);
                    }
            }
        }

        if (_lockedRedraw)
            Im.Tooltip.OnHover(HoveredFlags.AllowWhenDisabled,
                "The race can not be changed as this requires a redraw of the character, which is not supported for this actor."u8);
    }

    private void DrawBodyType()
    {
        if (_customize.BodyType.Value is 1)
            return;

        if (!ImEx.Button(_lockedRedraw
                    ? $"Body Type {_customize.BodyType.Value}"
                    : $"Reset Body Type {_customize.BodyType.Value} to Default",
                new Vector2(_raceSelectorWidth + _framedIconSize.X + Im.Style.ItemSpacing.X, 0),
                StringU8.Empty, _lockedRedraw))
            return;

        Changed             |= CustomizeFlag.BodyType;
        _customize.BodyType =  (CustomizeValue)1;
    }
}
