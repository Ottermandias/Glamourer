using Glamourer.GameData;
using OtterGui.Services;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Services;

public sealed class CustomizeService(
    HumanModelList humanModels,
    NpcCustomizeSet npcCustomizeSet,
    CustomizeManager manager)
    : IAsyncService
{
    public readonly HumanModelList   HumanModels     = humanModels;
    public readonly CustomizeManager Manager         = manager;
    public readonly NpcCustomizeSet  NpcCustomizeSet = npcCustomizeSet;

    public Task Awaiter { get; }
        = Task.WhenAll(humanModels.Awaiter, manager.Awaiter, npcCustomizeSet.Awaiter);

    public bool Finished
        => Awaiter.IsCompletedSuccessfully;

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

        if (applyWhich.HasFlag(CustomizeFlag.BodyType))
        {
            if (oldValues[CustomizeIndex.BodyType] != newValues[CustomizeIndex.BodyType])
            {
                ret.Set(CustomizeIndex.BodyType, newValues[CustomizeIndex.BodyType]);
                changed |= CustomizeFlag.BodyType;
            }

            applied |= CustomizeFlag.BodyType;
        }

        var set = Manager.GetSet(ret.Clan, ret.Gender);
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
        return Manager.GetSet(race, gender).Name;
    }

    /// <summary> Returns whether a clan is valid. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool IsClanValid(SubRace clan)
        => CustomizeManager.Clans.Contains(clan);

    /// <summary> Returns whether a gender is valid for the given race. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool IsGenderValid(Race race, Gender gender)
        => race is Race.Hrothgar ? gender == Gender.Male : CustomizeManager.Genders.Contains(gender);

    /// <inheritdoc cref="IsCustomizationValid(CustomizeSet,CustomizeValue,CustomizeIndex,CustomizeValue, out CustomizeData?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool IsCustomizationValid(CustomizeSet set, CustomizeValue face, CustomizeIndex type, CustomizeValue value)
        => IsCustomizationValid(set, face, type, value, out _);

    /// <summary> Returns whether a customization value is valid for a given clan/gender set and face. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool IsCustomizationValid(CustomizeSet set, CustomizeValue face, CustomizeIndex type, CustomizeValue value,
        out CustomizeData? data)
        => set.Validate(type, value, out data, face);

    /// <summary> Returns whether a customization value is valid for a given clan, gender and face. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool IsCustomizationValid(SubRace race, Gender gender, CustomizeValue face, CustomizeIndex type, CustomizeValue value)
        => IsCustomizationValid(Manager.GetSet(race, gender), face, type, value);

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

        if (CustomizeManager.Races.Contains(race))
        {
            actualRace = race;
            actualClan = CustomizeManager.Clans.FirstOrDefault(c => c.ToRace() == race, SubRace.Unknown);
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
        if (!CustomizeManager.Genders.Contains(gender))
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
    public static string ValidateCustomizeValue(CustomizeSet set, CustomizeValue face, CustomizeIndex index, CustomizeValue value,
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

        var set = Manager.GetSet(customize.Clan, customize.Gender);
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
        var set = Manager.GetSet(customize.Clan, customize.Gender);
        return FixValues(set, ref customize) | CustomizeFlag.Gender;
    }

    private static CustomizeFlag FixValues(CustomizeSet set, ref CustomizeArray customize)
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
