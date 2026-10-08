using Glamourer.GameData;
using Glamourer.Services;
using ImSharp;
using Luna;
using Penumbra.GameData.Enums;

namespace Glamourer.Gui;

internal sealed class ClanGenderFilter(CustomizeService customize)
{
    private static readonly Rgba32 FemaleTextColor = new(255, 175, 200, 255);
    private static readonly Rgba32 MaleTextColor   = new(160, 205, 255, 255);

    private static readonly SubRace[] Clans   = [SubRace.Unknown, .. CustomizeManager.Clans];
    private static readonly Gender[] Genders = [Gender.Unknown, Gender.Female, Gender.Male];

    private SubRace _clan   = SubRace.Unknown;
    private Gender  _gender = Gender.Unknown;

    public bool IsEmpty
        => _clan is SubRace.Unknown && _gender is Gender.Unknown;

    public bool Clear()
    {
        if (IsEmpty)
            return false;

        _clan   = SubRace.Unknown;
        _gender = Gender.Unknown;
        return true;
    }

    public bool Matches(SubRace clan, Gender gender)
        => (_clan is SubRace.Unknown || clan == _clan) && (_gender is Gender.Unknown || gender == _gender);

    public bool DrawSelectors(Vector2 availableRegion)
    {
        using var id = Im.Id.Push("ClanGenderFilters"u8);
        var clanWidth = Math.Max(0, availableRegion.X * 0.72f - Im.Style.ItemSpacing.X / 2);
        Im.Item.SetNextWidth(clanWidth);
        var changes = DrawClanCombo();
        Im.Line.Same();
        Im.Item.SetNextWidth(Math.Max(0, availableRegion.X - clanWidth - Im.Style.ItemSpacing.X));
        changes |= DrawGenderCombo();
        return changes;
    }

    private bool DrawClanCombo()
    {
        using var combo = _clan is SubRace.Unknown
            ? Im.Combo.Begin("##clan"u8, "Clan"u8, ComboFlags.HeightLargest)
            : Im.Combo.Begin("##clan"u8, customize.ClanName(_clan, _gender is Gender.Female ? Gender.Female : Gender.Male),
                ComboFlags.HeightLargest);
        var changes = HandleMouseWheel(ref _clan, Clans);
        if (!combo)
            return changes;

        if (Im.Selectable("All"u8, _clan is SubRace.Unknown) && _clan is not SubRace.Unknown)
        {
            _clan  = SubRace.Unknown;
            changes = true;
        }

        foreach (var clan in CustomizeManager.Clans)
        {
            if (Im.Selectable(customize.ClanName(clan, _gender is Gender.Female ? Gender.Female : Gender.Male), clan == _clan)
             && clan != _clan)
            {
                _clan  = clan;
                changes = true;
            }
        }

        return changes;
    }

    private bool DrawGenderCombo()
    {
        using var color = ImGuiColor.Text.Push(GenderTextColor(_gender), _gender is not Gender.Unknown);
        using var combo = _gender is Gender.Unknown
            ? Im.Combo.Begin("##gender"u8, "Gender"u8)
            : Im.Combo.Begin("##gender"u8, _gender.ToNameU8());
        color.Pop();
        var changes = HandleMouseWheel(ref _gender, Genders);
        if (!combo)
            return changes;

        foreach (var gender in Genders)
        {
            var name = gender is Gender.Unknown ? "All"u8 : gender.ToNameU8();
            using var itemColor = ImGuiColor.Text.Push(GenderTextColor(gender), gender is not Gender.Unknown);
            if (Im.Selectable(name, gender == _gender) && gender != _gender)
            {
                _gender = gender;
                changes = true;
            }
        }

        return changes;
    }

    private static Rgba32 GenderTextColor(Gender gender)
        => gender switch
        {
            Gender.Female => FemaleTextColor,
            Gender.Male   => MaleTextColor,
            _             => default,
        };

    private static bool HandleMouseWheel<T>(ref T value, T[] values) where T : struct, Enum
    {
        if (!Im.Item.Hovered() || !MouseWheelType.Control.CheckMouseWheel())
            return false;

        Im.Item.SetUsingMouseWheel();
        var delta = (int)Im.Io.MouseWheel;
        if (delta is 0)
            return false;

        var currentIndex = Array.IndexOf(values, value);
        var newIndex     = ImUtility.ApplyMouseWheelDelta(delta, currentIndex, values.Length);
        value = values[newIndex];
        return newIndex != currentIndex;
    }
}
