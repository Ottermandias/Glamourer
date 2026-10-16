using Dalamud.Interface;
using Glamourer.GameData;
using Glamourer.Services;
using ImSharp;
using Luna;
using Penumbra.GameData.Enums;

namespace Glamourer.Gui;

internal sealed class ClanGenderFilter(CustomizeService customize)
{
    private static readonly AwesomeIcon MaleIcon   = FontAwesomeIcon.Mars;
    private static readonly AwesomeIcon FemaleIcon = FontAwesomeIcon.Venus;

    private static readonly Rgba32 MaleTextColor   = new(160, 205, 255, 255);
    private static readonly Rgba32 FemaleTextColor = new(255, 175, 200, 255);

    [Flags]
    private enum ClanFlags : ushort
    {
        None            = 0,
        Midlander      = 1 << 0,
        Highlander     = 1 << 1,
        Wildwood       = 1 << 2,
        Duskwight      = 1 << 3,
        Plainsfolk     = 1 << 4,
        Dunesfolk      = 1 << 5,
        SeekerOfTheSun = 1 << 6,
        KeeperOfTheMoon = 1 << 7,
        Seawolf        = 1 << 8,
        Hellsguard     = 1 << 9,
        Raen           = 1 << 10,
        Xaela          = 1 << 11,
        Helion         = 1 << 12,
        Lost           = 1 << 13,
        Rava           = 1 << 14,
        Veena          = 1 << 15,
        All            = ushort.MaxValue,
    }

    private ClanFlags _clans = ClanFlags.All;
    private bool?     _male;

    public bool IsEmpty
        => _clans is ClanFlags.All && _male is null;

    public bool Clear()
    {
        if (IsEmpty)
            return false;

        _clans = ClanFlags.All;
        _male  = null;
        return true;
    }

    public bool Matches(SubRace clan, Gender gender)
    {
        var clanFlag      = ToFlag(clan);
        var clanMatches   = _clans is ClanFlags.All || clanFlag is not ClanFlags.None && _clans.HasFlag(clanFlag);
        var normalized    = NormalizeGender(gender);
        var genderMatches = _male switch
        {
            true  => normalized is Gender.Male,
            false => normalized is Gender.Female,
            null  => true,
        };
        return clanMatches && genderMatches;
    }

    public bool Draw()
    {
        using var id = Im.Id.Push("ClanGenderFilters"u8);
        var changes = DrawGender();
        Im.Dummy(ImEx.ScaledVectorY(8));
        changes |= DrawClans();
        return changes;
    }

    private bool DrawGender()
    {
        Im.Text("Gender"u8);
        var iconFontSize = Im.Style.FrameHeight - 2 * Im.Style.GlobalScale;
        var iconScale    = iconFontSize / Im.Style.TextHeight;
        var maleSize     = ImEx.Icon.CalculateSize(MaleIcon) * iconScale;
        var femaleSize   = ImEx.Icon.CalculateSize(FemaleIcon) * iconScale;
        var buttonSize = new Vector2(maleSize.X + femaleSize.X + Im.Style.ItemInnerSpacing.X + 2 * Im.Style.FramePadding.X,
            Im.Style.FrameHeight);
        var changes = Im.Button("##gender"u8, buttonSize);
        if (changes)
        {
            _male = _male switch
            {
                null  => true,
                true  => false,
                false => null,
            };
        }

        var drawMale     = _male is not false;
        var drawFemale   = _male is not true;
        var iconSpacing  = drawMale && drawFemale ? Im.Style.ItemInnerSpacing.X : 0;
        var contentWidth = (drawMale ? maleSize.X : 0) + (drawFemale ? femaleSize.X : 0) + iconSpacing;
        var position = Im.Item.UpperLeftCorner
          + new Vector2((buttonSize.X - contentWidth) / 2, (buttonSize.Y - Math.Max(maleSize.Y, femaleSize.Y)) / 2);
        if (drawMale)
        {
            Im.DrawList.Window.Text(AwesomeIcon.Font, iconFontSize, position, MaleTextColor, MaleIcon.Span);
            position.X += maleSize.X + iconSpacing;
        }

        if (drawFemale)
            Im.DrawList.Window.Text(AwesomeIcon.Font, iconFontSize, position, FemaleTextColor, FemaleIcon.Span);

        Im.Tooltip.OnHover(_male switch
        {
            true  => "Male\nClick to cycle gender filter."u8,
            false => "Female\nClick to cycle gender filter."u8,
            null  => "Both\nClick to cycle gender filter."u8,
        });
        return changes;
    }

    private bool DrawClans()
    {
        Im.Text("Clan"u8);
        var changes = Im.Checkbox("Everything"u8, ref _clans, ClanFlags.All);

        Im.Dummy(ImEx.ScaledVectorY(8));
        using (ImStyleDouble.ItemSpacing.PushY(2 * Im.Style.GlobalScale))
        {
            foreach (var clan in CustomizeManager.Clans)
            {
                var flag = ToFlag(clan);
                changes |= Im.Checkbox(customize.ClanName(clan, Gender.Male), ref _clans, flag);
            }
        }

        return changes;
    }

    private static Gender NormalizeGender(Gender gender)
        => gender switch
        {
            Gender.MaleNpc   => Gender.Male,
            Gender.FemaleNpc => Gender.Female,
            _                => gender,
        };

    private static ClanFlags ToFlag(SubRace clan)
        => clan switch
        {
            SubRace.Midlander       => ClanFlags.Midlander,
            SubRace.Highlander      => ClanFlags.Highlander,
            SubRace.Wildwood        => ClanFlags.Wildwood,
            SubRace.Duskwight       => ClanFlags.Duskwight,
            SubRace.Plainsfolk      => ClanFlags.Plainsfolk,
            SubRace.Dunesfolk       => ClanFlags.Dunesfolk,
            SubRace.SeekerOfTheSun  => ClanFlags.SeekerOfTheSun,
            SubRace.KeeperOfTheMoon => ClanFlags.KeeperOfTheMoon,
            SubRace.Seawolf         => ClanFlags.Seawolf,
            SubRace.Hellsguard      => ClanFlags.Hellsguard,
            SubRace.Raen            => ClanFlags.Raen,
            SubRace.Xaela           => ClanFlags.Xaela,
            SubRace.Helion          => ClanFlags.Helion,
            SubRace.Lost            => ClanFlags.Lost,
            SubRace.Rava            => ClanFlags.Rava,
            SubRace.Veena           => ClanFlags.Veena,
            _                       => ClanFlags.None,
        };
}
