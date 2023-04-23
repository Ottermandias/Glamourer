using System;
using Glamourer.Customization;
using Glamourer.Services;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Util;

public static class CustomizeExtensions
{
    // In languages other than english the actual clan name may depend on gender.
    public static string ClanName(ICustomizationManager customization, SubRace race, Gender gender)
    {
        if (gender == Gender.FemaleNpc)
            gender = Gender.Female;
        if (gender == Gender.MaleNpc)
            gender = Gender.Male;
        return (gender, race) switch
        {
            (Gender.Male, SubRace.Midlander)         => customization.GetName(CustomName.MidlanderM),
            (Gender.Male, SubRace.Highlander)        => customization.GetName(CustomName.HighlanderM),
            (Gender.Male, SubRace.Wildwood)          => customization.GetName(CustomName.WildwoodM),
            (Gender.Male, SubRace.Duskwight)         => customization.GetName(CustomName.DuskwightM),
            (Gender.Male, SubRace.Plainsfolk)        => customization.GetName(CustomName.PlainsfolkM),
            (Gender.Male, SubRace.Dunesfolk)         => customization.GetName(CustomName.DunesfolkM),
            (Gender.Male, SubRace.SeekerOfTheSun)    => customization.GetName(CustomName.SeekerOfTheSunM),
            (Gender.Male, SubRace.KeeperOfTheMoon)   => customization.GetName(CustomName.KeeperOfTheMoonM),
            (Gender.Male, SubRace.Seawolf)           => customization.GetName(CustomName.SeawolfM),
            (Gender.Male, SubRace.Hellsguard)        => customization.GetName(CustomName.HellsguardM),
            (Gender.Male, SubRace.Raen)              => customization.GetName(CustomName.RaenM),
            (Gender.Male, SubRace.Xaela)             => customization.GetName(CustomName.XaelaM),
            (Gender.Male, SubRace.Helion)            => customization.GetName(CustomName.HelionM),
            (Gender.Male, SubRace.Lost)              => customization.GetName(CustomName.LostM),
            (Gender.Male, SubRace.Rava)              => customization.GetName(CustomName.RavaM),
            (Gender.Male, SubRace.Veena)             => customization.GetName(CustomName.VeenaM),
            (Gender.Female, SubRace.Midlander)       => customization.GetName(CustomName.MidlanderF),
            (Gender.Female, SubRace.Highlander)      => customization.GetName(CustomName.HighlanderF),
            (Gender.Female, SubRace.Wildwood)        => customization.GetName(CustomName.WildwoodF),
            (Gender.Female, SubRace.Duskwight)       => customization.GetName(CustomName.DuskwightF),
            (Gender.Female, SubRace.Plainsfolk)      => customization.GetName(CustomName.PlainsfolkF),
            (Gender.Female, SubRace.Dunesfolk)       => customization.GetName(CustomName.DunesfolkF),
            (Gender.Female, SubRace.SeekerOfTheSun)  => customization.GetName(CustomName.SeekerOfTheSunF),
            (Gender.Female, SubRace.KeeperOfTheMoon) => customization.GetName(CustomName.KeeperOfTheMoonF),
            (Gender.Female, SubRace.Seawolf)         => customization.GetName(CustomName.SeawolfF),
            (Gender.Female, SubRace.Hellsguard)      => customization.GetName(CustomName.HellsguardF),
            (Gender.Female, SubRace.Raen)            => customization.GetName(CustomName.RaenF),
            (Gender.Female, SubRace.Xaela)           => customization.GetName(CustomName.XaelaF),
            (Gender.Female, SubRace.Helion)          => customization.GetName(CustomName.HelionM),
            (Gender.Female, SubRace.Lost)            => customization.GetName(CustomName.LostM),
            (Gender.Female, SubRace.Rava)            => customization.GetName(CustomName.RavaF),
            (Gender.Female, SubRace.Veena)           => customization.GetName(CustomName.VeenaF),
            _                                        => throw new ArgumentOutOfRangeException(nameof(race), race, null),
        };
    }

    public static string ClanName(this Customize customize, ICustomizationManager customization)
        => ClanName(customization, customize.Clan, customize.Gender);


    // Change a gender and fix up all required customizations afterwards.
    public static CustomizeFlag ChangeGender(this Customize customize, CharacterEquip equip, Gender gender, ItemManager items, ICustomizationManager customization)
    {
        if (customize.Gender == gender)
            return 0;

        FixRestrictedGear(items, customize, equip, gender, customize.Race);
        customize.Gender = gender;
        return CustomizeFlag.Gender | FixUpAttributes(customization, customize);
    }

    // Change a race and fix up all required customizations afterwards.
    public static CustomizeFlag ChangeRace(this Customize customize, CharacterEquip equip, SubRace clan, ItemManager items, ICustomizationManager customization)
    {
        if (customize.Clan == clan)
            return 0;

        var race   = clan.ToRace();
        var gender = race == Race.Hrothgar ? Gender.Male : customize.Gender; // TODO Female Hrothgar
        FixRestrictedGear(items, customize, equip, gender, race);
        var flags = CustomizeFlag.Race | CustomizeFlag.Clan;
        if (gender != customize.Gender)
            flags |= CustomizeFlag.Gender;
        customize.Gender = gender;
        customize.Race   = race;
        customize.Clan   = clan;
        return flags | FixUpAttributes(customization, customize);
    }

    // Go through a whole customization struct and fix up all settings that need fixing.
    private static CustomizeFlag FixUpAttributes(ICustomizationManager customization, Customize customize)
    {
        var           set   = customization.GetList(customize.Clan, customize.Gender);
        CustomizeFlag flags = 0;
        foreach (CustomizeIndex id in Enum.GetValues(typeof(CustomizeIndex)))
        {
            switch (id)
            {
                case CustomizeIndex.Race:       break;
                case CustomizeIndex.Clan:       break;
                case CustomizeIndex.BodyType:   break;
                case CustomizeIndex.Gender:     break;
                case CustomizeIndex.Highlights: break;
                case CustomizeIndex.Face:       break;
                default:
                    var count = set.Count(id);
                    if (set.DataByValue(id, customize[id], out _, customize.Face) < 0)
                    {
                        customize[id] =  count == 0 ? CustomizeValue.Zero : set.Data(id, 0).Value;
                        flags         |= id.ToFlag();
                    }

                    break;
            }
        }

        return flags;
    }

    private static void FixRestrictedGear(ItemManager items, Customize customize, CharacterEquip equip, Gender gender, Race race)
    {
        if (!equip || race == customize.Race && gender == customize.Gender)
            return;

        foreach (var slot in EquipSlotExtensions.EqdpSlots)
            (_, equip[slot]) = items.ResolveRestrictedGear(equip[slot], slot, race, gender);
    }
}
