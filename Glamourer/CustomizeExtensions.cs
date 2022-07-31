using System;
using Glamourer.Customization;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer;

public static unsafe class CustomizeExtensions
{
    // In languages other than english the actual clan name may depend on gender.
    public static string ClanName(SubRace race, Gender gender)
    {
        if (gender == Gender.FemaleNpc)
            gender = Gender.Female;
        if (gender == Gender.MaleNpc)
            gender = Gender.Male;
        return (gender, race) switch
        {
            (Gender.Male, SubRace.Midlander)         => Glamourer.Customization.GetName(CustomName.MidlanderM),
            (Gender.Male, SubRace.Highlander)        => Glamourer.Customization.GetName(CustomName.HighlanderM),
            (Gender.Male, SubRace.Wildwood)          => Glamourer.Customization.GetName(CustomName.WildwoodM),
            (Gender.Male, SubRace.Duskwight)         => Glamourer.Customization.GetName(CustomName.DuskwightM),
            (Gender.Male, SubRace.Plainsfolk)        => Glamourer.Customization.GetName(CustomName.PlainsfolkM),
            (Gender.Male, SubRace.Dunesfolk)         => Glamourer.Customization.GetName(CustomName.DunesfolkM),
            (Gender.Male, SubRace.SeekerOfTheSun)    => Glamourer.Customization.GetName(CustomName.SeekerOfTheSunM),
            (Gender.Male, SubRace.KeeperOfTheMoon)   => Glamourer.Customization.GetName(CustomName.KeeperOfTheMoonM),
            (Gender.Male, SubRace.Seawolf)           => Glamourer.Customization.GetName(CustomName.SeawolfM),
            (Gender.Male, SubRace.Hellsguard)        => Glamourer.Customization.GetName(CustomName.HellsguardM),
            (Gender.Male, SubRace.Raen)              => Glamourer.Customization.GetName(CustomName.RaenM),
            (Gender.Male, SubRace.Xaela)             => Glamourer.Customization.GetName(CustomName.XaelaM),
            (Gender.Male, SubRace.Helion)            => Glamourer.Customization.GetName(CustomName.HelionM),
            (Gender.Male, SubRace.Lost)              => Glamourer.Customization.GetName(CustomName.LostM),
            (Gender.Male, SubRace.Rava)              => Glamourer.Customization.GetName(CustomName.RavaM),
            (Gender.Male, SubRace.Veena)             => Glamourer.Customization.GetName(CustomName.VeenaM),
            (Gender.Female, SubRace.Midlander)       => Glamourer.Customization.GetName(CustomName.MidlanderF),
            (Gender.Female, SubRace.Highlander)      => Glamourer.Customization.GetName(CustomName.HighlanderF),
            (Gender.Female, SubRace.Wildwood)        => Glamourer.Customization.GetName(CustomName.WildwoodF),
            (Gender.Female, SubRace.Duskwight)       => Glamourer.Customization.GetName(CustomName.DuskwightF),
            (Gender.Female, SubRace.Plainsfolk)      => Glamourer.Customization.GetName(CustomName.PlainsfolkF),
            (Gender.Female, SubRace.Dunesfolk)       => Glamourer.Customization.GetName(CustomName.DunesfolkF),
            (Gender.Female, SubRace.SeekerOfTheSun)  => Glamourer.Customization.GetName(CustomName.SeekerOfTheSunF),
            (Gender.Female, SubRace.KeeperOfTheMoon) => Glamourer.Customization.GetName(CustomName.KeeperOfTheMoonF),
            (Gender.Female, SubRace.Seawolf)         => Glamourer.Customization.GetName(CustomName.SeawolfF),
            (Gender.Female, SubRace.Hellsguard)      => Glamourer.Customization.GetName(CustomName.HellsguardF),
            (Gender.Female, SubRace.Raen)            => Glamourer.Customization.GetName(CustomName.RaenF),
            (Gender.Female, SubRace.Xaela)           => Glamourer.Customization.GetName(CustomName.XaelaF),
            (Gender.Female, SubRace.Helion)          => Glamourer.Customization.GetName(CustomName.HelionM),
            (Gender.Female, SubRace.Lost)            => Glamourer.Customization.GetName(CustomName.LostM),
            (Gender.Female, SubRace.Rava)            => Glamourer.Customization.GetName(CustomName.RavaF),
            (Gender.Female, SubRace.Veena)           => Glamourer.Customization.GetName(CustomName.VeenaF),
            _                                        => throw new ArgumentOutOfRangeException(nameof(race), race, null),
        };
    }

    public static string ClanName(this Customize customize)
        => ClanName(customize.Clan, customize.Gender);
}
