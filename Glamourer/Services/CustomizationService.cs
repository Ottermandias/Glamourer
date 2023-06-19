using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Dalamud.Data;
using Dalamud.Plugin;
using Glamourer.Customization;
using Penumbra.GameData.Enums;

namespace Glamourer.Services;

public sealed class CustomizationService : AsyncServiceWrapper<ICustomizationManager>
{
    public CustomizationService(DalamudPluginInterface pi, DataManager gameData)
        : base(nameof(CustomizationService), () => CustomizationManager.Create(pi, gameData))
    { }

    /// <summary> In languages other than english the actual clan name may depend on gender. </summary>
    public string ClanName(SubRace race, Gender gender)
    {
        if (gender == Gender.FemaleNpc)
            gender = Gender.Female;
        if (gender == Gender.MaleNpc)
            gender = Gender.Male;
        return (gender, race) switch
        {
            (Gender.Male, SubRace.Midlander)         => AwaitedService.GetName(CustomName.MidlanderM),
            (Gender.Male, SubRace.Highlander)        => AwaitedService.GetName(CustomName.HighlanderM),
            (Gender.Male, SubRace.Wildwood)          => AwaitedService.GetName(CustomName.WildwoodM),
            (Gender.Male, SubRace.Duskwight)         => AwaitedService.GetName(CustomName.DuskwightM),
            (Gender.Male, SubRace.Plainsfolk)        => AwaitedService.GetName(CustomName.PlainsfolkM),
            (Gender.Male, SubRace.Dunesfolk)         => AwaitedService.GetName(CustomName.DunesfolkM),
            (Gender.Male, SubRace.SeekerOfTheSun)    => AwaitedService.GetName(CustomName.SeekerOfTheSunM),
            (Gender.Male, SubRace.KeeperOfTheMoon)   => AwaitedService.GetName(CustomName.KeeperOfTheMoonM),
            (Gender.Male, SubRace.Seawolf)           => AwaitedService.GetName(CustomName.SeawolfM),
            (Gender.Male, SubRace.Hellsguard)        => AwaitedService.GetName(CustomName.HellsguardM),
            (Gender.Male, SubRace.Raen)              => AwaitedService.GetName(CustomName.RaenM),
            (Gender.Male, SubRace.Xaela)             => AwaitedService.GetName(CustomName.XaelaM),
            (Gender.Male, SubRace.Helion)            => AwaitedService.GetName(CustomName.HelionM),
            (Gender.Male, SubRace.Lost)              => AwaitedService.GetName(CustomName.LostM),
            (Gender.Male, SubRace.Rava)              => AwaitedService.GetName(CustomName.RavaM),
            (Gender.Male, SubRace.Veena)             => AwaitedService.GetName(CustomName.VeenaM),
            (Gender.Female, SubRace.Midlander)       => AwaitedService.GetName(CustomName.MidlanderF),
            (Gender.Female, SubRace.Highlander)      => AwaitedService.GetName(CustomName.HighlanderF),
            (Gender.Female, SubRace.Wildwood)        => AwaitedService.GetName(CustomName.WildwoodF),
            (Gender.Female, SubRace.Duskwight)       => AwaitedService.GetName(CustomName.DuskwightF),
            (Gender.Female, SubRace.Plainsfolk)      => AwaitedService.GetName(CustomName.PlainsfolkF),
            (Gender.Female, SubRace.Dunesfolk)       => AwaitedService.GetName(CustomName.DunesfolkF),
            (Gender.Female, SubRace.SeekerOfTheSun)  => AwaitedService.GetName(CustomName.SeekerOfTheSunF),
            (Gender.Female, SubRace.KeeperOfTheMoon) => AwaitedService.GetName(CustomName.KeeperOfTheMoonF),
            (Gender.Female, SubRace.Seawolf)         => AwaitedService.GetName(CustomName.SeawolfF),
            (Gender.Female, SubRace.Hellsguard)      => AwaitedService.GetName(CustomName.HellsguardF),
            (Gender.Female, SubRace.Raen)            => AwaitedService.GetName(CustomName.RaenF),
            (Gender.Female, SubRace.Xaela)           => AwaitedService.GetName(CustomName.XaelaF),
            (Gender.Female, SubRace.Helion)          => AwaitedService.GetName(CustomName.HelionM),
            (Gender.Female, SubRace.Lost)            => AwaitedService.GetName(CustomName.LostM),
            (Gender.Female, SubRace.Rava)            => AwaitedService.GetName(CustomName.RavaF),
            (Gender.Female, SubRace.Veena)           => AwaitedService.GetName(CustomName.VeenaF),
            _                                        => "Unknown",
        };
    }

    /// <summary> Returns whether a clan is valid. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool IsClanValid(SubRace clan)
        => AwaitedService.Clans.Contains(clan);

    /// <summary> Returns whether a gender is valid for the given race. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool IsGenderValid(Race race, Gender gender)
        => race is Race.Hrothgar ? gender == Gender.Male : AwaitedService.Genders.Contains(gender);

    /// <summary> Returns whether a customization value is valid for a given clan/gender set and face. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool IsCustomizationValid(CustomizationSet set, CustomizeValue face, CustomizeIndex type, CustomizeValue value)
        => set.DataByValue(type, value, out _, face) >= 0;

    /// <summary> Returns whether a customization value is valid for a given clan, gender and face. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool IsCustomizationValid(SubRace race, Gender gender, CustomizeValue face, CustomizeIndex type, CustomizeValue value)
        => AwaitedService.GetList(race, gender).DataByValue(type, value, out _, face) >= 0;

    /// <summary>
    /// Check that the given race and clan are valid.
    /// The returned race and clan fit together and are valid.
    /// The return value is an empty string if everything was correct and a warning otherwise.
    /// </summary>
    public string ValidateClan(SubRace clan, Race race, out Race actualRace, out SubRace actualClan)
    {
        if (IsClanValid(clan))
        {
            actualClan = clan;
            actualRace = actualClan.ToRace();
            if (race != actualRace)
                return $"The race {race.ToName()} does not correspond to the clan {clan.ToName()}, changed to {actualRace.ToName()}.";

            return string.Empty;
        }

        if (AwaitedService.Races.Contains(race))
        {
            actualRace = race;
            actualClan = AwaitedService.Clans.FirstOrDefault(c => c.ToRace() == race, SubRace.Unknown);
            // This should not happen.
            if (actualClan == SubRace.Unknown)
            {
                actualRace = Race.Hyur;
                actualClan = SubRace.Midlander;
                return
                    $"The clan {clan.ToName()} is invalid and the race {race.ToName()} does not correspond to any clan, reset to {Race.Hyur.ToName()} {SubRace.Midlander.ToName()}.";
            }

            return $"The clan {clan.ToName()} is invalid, but the race {race.ToName()} is known, reset to {actualClan.ToName()}.";
        }

        actualRace = Race.Hyur;
        actualClan = SubRace.Midlander;
        return
            $"Both the clan {clan.ToName()} and the race {race.ToName()} are invalid, reset to {Race.Hyur.ToName()} {SubRace.Midlander.ToName()}.";
    }

    /// <summary>
    /// Check that the given gender is valid for that race.
    /// The returned gender is valid for the race.
    /// The return value is an empty string if everything was correct and a warning otherwise.
    /// </summary>
    public string ValidateGender(Race race, Gender gender, out Gender actualGender)
    {
        if (!AwaitedService.Genders.Contains(gender))
        {
            actualGender = Gender.Male;
            return $"The gender {gender.ToName()} is unknown, reset to {Gender.Male.ToName()}.";
        }

        // TODO: Female Hrothgar
        if (gender is Gender.Female && race is Race.Hrothgar)
        {
            actualGender = Gender.Male;
            return $"{Race.Hrothgar.ToName()} do not currently support {Gender.Female.ToName()} characters, reset to {Gender.Male.ToName()}.";
        }

        actualGender = gender;
        return string.Empty;
    }

    /// <summary>
    /// Check that the given model id is valid.
    /// The returned model id is 0.
    /// The return value is an empty string if everything was correct and a warning otherwise.
    /// </summary>
    public string ValidateModelId(uint modelId, out uint actualModelId)
    {
        actualModelId = 0;
        return modelId != 0 ? $"Model IDs different from 0 are not currently allowed, reset {modelId} to 0." : string.Empty;
    }

    /// <summary>
    /// Validate a single customization value against a given set of race and gender (and face).
    /// The returned actualValue is either the correct value or the one with index 0.
    /// The return value is an empty string or a warning message.
    /// </summary>
    public static string ValidateCustomizeValue(CustomizationSet set, CustomizeValue face, CustomizeIndex index, CustomizeValue value,
        out CustomizeValue actualValue)
    {
        if (IsCustomizationValid(set, face, index, value))
        {
            actualValue = value;
            return string.Empty;
        }

        var name     = set.Option(index);
        var newValue = set.Data(index, 0, face);
        actualValue = newValue.Value;
        return
            $"Customization {name} for {set.Race.ToName()} {set.Gender.ToName()}s does not support value {value.Value}, reset to {newValue.Value.Value}.";
    }

    /// <summary> Change a clan while keeping all other customizations valid. </summary>
    public bool ChangeClan(ref Customize customize, SubRace newClan)
    {
        if (customize.Clan == newClan)
            return false;

        if (ValidateClan(newClan, newClan.ToRace(), out var newRace, out newClan).Length > 0)
            return false;

        customize.Race = newRace;
        customize.Clan = newClan;

        // TODO Female Hrothgar
        if (newRace == Race.Hrothgar)
            customize.Gender = Gender.Male;

        var set = AwaitedService.GetList(customize.Clan, customize.Gender);
        FixValues(set, ref customize);

        return true;
    }

    /// <summary> Change a gender while keeping all other customizations valid. </summary>
    public bool ChangeGender(ref Customize customize, Gender newGender)
    {
        if (customize.Gender == newGender)
            return false;

        // TODO Female Hrothgar
        if (customize.Race is Race.Hrothgar)
            return false;

        if (ValidateGender(customize.Race, newGender, out newGender).Length > 0)
            return false;

        customize.Gender = newGender;
        var set = AwaitedService.GetList(customize.Clan, customize.Gender);
        FixValues(set, ref customize);

        return true;
    }

    private static void FixValues(CustomizationSet set, ref Customize customize)
    {
        foreach (var idx in Enum.GetValues<CustomizeIndex>().Where(set.IsAvailable))
        {
            if (ValidateCustomizeValue(set, customize.Face, idx, customize[idx], out var fixedValue).Length > 0)
                customize[idx] = fixedValue;
        }
    }
}
