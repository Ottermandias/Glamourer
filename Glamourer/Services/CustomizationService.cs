using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using Glamourer.GameData;
using OtterGui.Services;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Services;

public sealed class CustomizationService(
    ITextureProvider textures,
    IDataManager gameData,
    HumanModelList humanModels,
    IPluginLog log,
    NpcCustomizeSet npcCustomizeSet)
    : IAsyncService
{
    public readonly HumanModelList HumanModels = humanModels;

    private ICustomizationManager? _service;

    private readonly Task<ICustomizationManager> _task = Task.WhenAll(humanModels.Awaiter, npcCustomizeSet.Awaiter)
        .ContinueWith(_ => CustomizationManager.Create(textures, gameData, log, npcCustomizeSet));

    public ICustomizationManager Service
        => _service ??= _task.Result;

    public Task Awaiter
        => _task;

    public (CustomizeArray NewValue, CustomizeFlag Applied, CustomizeFlag Changed) Combine(CustomizeArray oldValues, CustomizeArray newValues,
        CustomizeFlag applyWhich, bool allowUnknown)
    {
        CustomizeFlag applied = 0;
        CustomizeFlag changed = 0;
        var           ret     = oldValues;
        if (applyWhich.HasFlag(CustomizeFlag.Clan))
        {
            changed |= ChangeClan(ref ret, newValues.Clan);
            applied |= CustomizeFlag.Clan;
        }

        if (applyWhich.HasFlag(CustomizeFlag.Gender))
            if (ret.Race is not Race.Hrothgar || newValues.Gender is not Gender.Female)
            {
                changed |= ChangeGender(ref ret, newValues.Gender);
                applied |= CustomizeFlag.Gender;
            }


        var set = Service.GetList(ret.Clan, ret.Gender);
        applyWhich = applyWhich.FixApplication(set);
        foreach (var index in CustomizationExtensions.AllBasic)
        {
            var flag = index.ToFlag();
            if (!applyWhich.HasFlag(flag))
                continue;

            var value = newValues[index];
            if (allowUnknown || IsCustomizationValid(set, ret.Face, index, value))
            {
                if (ret[index].Value != value.Value)
                    changed |= flag;
                ret[index] =  value;
                applied    |= flag;
            }
        }

        return (ret, applied, changed);
    }

    /// <summary> In languages other than english the actual clan name may depend on gender. </summary>
    public string ClanName(SubRace race, Gender gender)
    {
        if (gender == Gender.FemaleNpc)
            gender = Gender.Female;
        if (gender == Gender.MaleNpc)
            gender = Gender.Male;
        return (gender, race) switch
        {
            (Gender.Male, SubRace.Midlander)         => Service.GetName(CustomName.MidlanderM),
            (Gender.Male, SubRace.Highlander)        => Service.GetName(CustomName.HighlanderM),
            (Gender.Male, SubRace.Wildwood)          => Service.GetName(CustomName.WildwoodM),
            (Gender.Male, SubRace.Duskwight)         => Service.GetName(CustomName.DuskwightM),
            (Gender.Male, SubRace.Plainsfolk)        => Service.GetName(CustomName.PlainsfolkM),
            (Gender.Male, SubRace.Dunesfolk)         => Service.GetName(CustomName.DunesfolkM),
            (Gender.Male, SubRace.SeekerOfTheSun)    => Service.GetName(CustomName.SeekerOfTheSunM),
            (Gender.Male, SubRace.KeeperOfTheMoon)   => Service.GetName(CustomName.KeeperOfTheMoonM),
            (Gender.Male, SubRace.Seawolf)           => Service.GetName(CustomName.SeawolfM),
            (Gender.Male, SubRace.Hellsguard)        => Service.GetName(CustomName.HellsguardM),
            (Gender.Male, SubRace.Raen)              => Service.GetName(CustomName.RaenM),
            (Gender.Male, SubRace.Xaela)             => Service.GetName(CustomName.XaelaM),
            (Gender.Male, SubRace.Helion)            => Service.GetName(CustomName.HelionM),
            (Gender.Male, SubRace.Lost)              => Service.GetName(CustomName.LostM),
            (Gender.Male, SubRace.Rava)              => Service.GetName(CustomName.RavaM),
            (Gender.Male, SubRace.Veena)             => Service.GetName(CustomName.VeenaM),
            (Gender.Female, SubRace.Midlander)       => Service.GetName(CustomName.MidlanderF),
            (Gender.Female, SubRace.Highlander)      => Service.GetName(CustomName.HighlanderF),
            (Gender.Female, SubRace.Wildwood)        => Service.GetName(CustomName.WildwoodF),
            (Gender.Female, SubRace.Duskwight)       => Service.GetName(CustomName.DuskwightF),
            (Gender.Female, SubRace.Plainsfolk)      => Service.GetName(CustomName.PlainsfolkF),
            (Gender.Female, SubRace.Dunesfolk)       => Service.GetName(CustomName.DunesfolkF),
            (Gender.Female, SubRace.SeekerOfTheSun)  => Service.GetName(CustomName.SeekerOfTheSunF),
            (Gender.Female, SubRace.KeeperOfTheMoon) => Service.GetName(CustomName.KeeperOfTheMoonF),
            (Gender.Female, SubRace.Seawolf)         => Service.GetName(CustomName.SeawolfF),
            (Gender.Female, SubRace.Hellsguard)      => Service.GetName(CustomName.HellsguardF),
            (Gender.Female, SubRace.Raen)            => Service.GetName(CustomName.RaenF),
            (Gender.Female, SubRace.Xaela)           => Service.GetName(CustomName.XaelaF),
            (Gender.Female, SubRace.Helion)          => Service.GetName(CustomName.HelionM),
            (Gender.Female, SubRace.Lost)            => Service.GetName(CustomName.LostM),
            (Gender.Female, SubRace.Rava)            => Service.GetName(CustomName.RavaF),
            (Gender.Female, SubRace.Veena)           => Service.GetName(CustomName.VeenaF),
            _                                        => "Unknown",
        };
    }

    /// <summary> Returns whether a clan is valid. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool IsClanValid(SubRace clan)
        => Service.Clans.Contains(clan);

    /// <summary> Returns whether a gender is valid for the given race. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool IsGenderValid(Race race, Gender gender)
        => race is Race.Hrothgar ? gender == Gender.Male : Service.Genders.Contains(gender);

    /// <inheritdoc cref="IsCustomizationValid(CustomizationSet,CustomizeValue,CustomizeIndex,CustomizeValue, out CustomizeData?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool IsCustomizationValid(CustomizationSet set, CustomizeValue face, CustomizeIndex type, CustomizeValue value)
        => IsCustomizationValid(set, face, type, value, out _);

    /// <summary> Returns whether a customization value is valid for a given clan/gender set and face. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool IsCustomizationValid(CustomizationSet set, CustomizeValue face, CustomizeIndex type, CustomizeValue value,
        out CustomizeData? data)
        => set.Validate(type, value, out data, face);

    /// <summary> Returns whether a customization value is valid for a given clan, gender and face. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool IsCustomizationValid(SubRace race, Gender gender, CustomizeValue face, CustomizeIndex type, CustomizeValue value)
        => IsCustomizationValid(Service.GetList(race, gender), face, type, value);

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

        if (Service.Races.Contains(race))
        {
            actualRace = race;
            actualClan = Service.Clans.FirstOrDefault(c => c.ToRace() == race, SubRace.Unknown);
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
        if (!Service.Genders.Contains(gender))
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
    /// The returned model id is 0 if it is not.
    /// The return value is an empty string if everything was correct and a warning otherwise.
    /// </summary>
    public string ValidateModelId(uint modelId, out uint actualModelId, out bool isHuman)
    {
        if (modelId >= HumanModels.Count)
        {
            actualModelId = 0;
            isHuman       = true;
            return $"Model ID {modelId} is not an existing model, reset to 0.";
        }

        actualModelId = modelId;
        isHuman       = HumanModels.IsHuman(modelId);
        return string.Empty;
    }

    /// <summary>
    /// Validate a single customization value against a given set of race and gender (and face).
    /// The returned actualValue is either the correct value or the one with index 0.
    /// The return value is an empty string or a warning message.
    /// </summary>
    public static string ValidateCustomizeValue(CustomizationSet set, CustomizeValue face, CustomizeIndex index, CustomizeValue value,
        out CustomizeValue actualValue, bool allowUnknown)
    {
        if (allowUnknown || IsCustomizationValid(set, face, index, value))
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
    public CustomizeFlag ChangeClan(ref CustomizeArray customize, SubRace newClan)
    {
        if (customize.Clan == newClan)
            return 0;

        if (ValidateClan(newClan, newClan.ToRace(), out var newRace, out newClan).Length > 0)
            return 0;

        var flags = CustomizeFlag.Clan | CustomizeFlag.Race;
        customize.Race = newRace;
        customize.Clan = newClan;

        // TODO Female Hrothgar
        if (newRace == Race.Hrothgar)
        {
            customize.Gender =  Gender.Male;
            flags            |= CustomizeFlag.Gender;
        }

        var set = Service.GetList(customize.Clan, customize.Gender);
        return FixValues(set, ref customize) | flags;
    }

    /// <summary> Change a gender while keeping all other customizations valid. </summary>
    public CustomizeFlag ChangeGender(ref CustomizeArray customize, Gender newGender)
    {
        if (customize.Gender == newGender)
            return 0;

        // TODO Female Hrothgar
        if (customize.Race is Race.Hrothgar)
            return 0;

        if (ValidateGender(customize.Race, newGender, out newGender).Length > 0)
            return 0;

        customize.Gender = newGender;
        var set = Service.GetList(customize.Clan, customize.Gender);
        return FixValues(set, ref customize) | CustomizeFlag.Gender;
    }

    private static CustomizeFlag FixValues(CustomizationSet set, ref CustomizeArray customize)
    {
        CustomizeFlag flags = 0;
        foreach (var idx in CustomizationExtensions.AllBasic)
        {
            if (set.IsAvailable(idx))
            {
                if (ValidateCustomizeValue(set, customize.Face, idx, customize[idx], out var fixedValue, false).Length > 0)
                {
                    customize[idx] =  fixedValue;
                    flags          |= idx.ToFlag();
                }
            }
            else
            {
                customize[idx] =  CustomizeValue.Zero;
                flags          |= idx.ToFlag();
            }
        }

        return flags;
    }
}
