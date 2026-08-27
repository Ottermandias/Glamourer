using System.Text.Json;
using Glamourer.Automation;
using ImSharp;
using Luna;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

namespace Glamourer.Designs;

public readonly record struct DesignConditions(JobGroup Jobs, short GearsetIndex = -1, bool? Constant = null)
{
    public static readonly DesignConditions AlwaysTrue = new(default, Constant: true);

    public DesignConditionData Data
        => Constant switch
        {
            true => new DesignConditionData(1),
            null => new DesignConditionData(Jobs.Id.Id, GearsetIndex),
            // "Always False" is not supposed to be constructed at the time of writing, and is therefore not supported for serialization.
            false => throw new NotSupportedException(),
        };

    public JobFlag JobFlags
        => Constant switch
        {
            true  => JobFlag.All,
            false => 0,
            null  => Jobs.Flags,
        };

    public unsafe bool Match(Actor actor)
    {
        if (Constant is { } value)
            return value;

        if (!actor.IsCharacter)
            return false;

        return GearsetIndex < 0
            ? Jobs.Fits(actor.AsCharacter->CharacterData.ClassJob)
            : AutoDesignApplier.CheckGearset(GearsetIndex);
    }

    public bool Match(JobId job, short gearset)
        => Constant
         ?? (GearsetIndex < 0
                ? Jobs.Fits(job)
                : gearset == GearsetIndex);

    public override string ToString()
        => Constant is { } value ? $"Always {value}" :
            GearsetIndex is -1   ? $"Jobs: {Jobs.Name}" : $"Gearset: {GearsetIndex}";

    public StringU8 ToJobsRestrictionString()
        => Constant is null && GearsetIndex is -1 ? Jobs.Name : StringU8.Empty;

    public StringU8 ToGearSetRestrictionString()
        => Constant is not null || GearsetIndex is -1 ? StringU8.Empty : new StringU8($"{GearsetIndex}");
}

public readonly record struct DesignConditionData(int JobGroupId, short GearsetIndex = -1)
{
    public void Serialize(Utf8JsonWriter j, ReadOnlySpan<byte> propertyName)
    {
        if (GearsetIndex < 0 && JobGroupId <= 0)
            return;

        j.WritePropertyName(propertyName);
        Serialize(j);
    }

    public void Serialize(Utf8JsonWriter j)
    {
        j.WriteStartObject();
        if (GearsetIndex >= 0)
            j.WriteNumber("Gearset"u8, GearsetIndex);
        if (JobGroupId > 0)
            j.WriteNumber("JobGroup"u8, JobGroupId);
        j.WriteEndObject();
    }

    public static bool TryDeserialize(ref Utf8JsonReader j, out DesignConditionData data)
    {
        data = new DesignConditionData(0);
        if (j.TokenType is not JsonTokenType.StartObject and not JsonTokenType.Null)
            return false;

        var limit = j.CreateObjectLimit();
        while (limit.Read(ref j))
        {
            if (j.TokenType is not JsonTokenType.PropertyName)
                continue;

            if (j.NumberProperty("Gearset"u8, out short gear))
                data = data with { GearsetIndex = gear };
            else if (j.NumberProperty("JobGroup"u8, out int job))
                data = data with { JobGroupId = job };
            else
                j.Skip();
        }

        return true;
    }

    public static DesignConditionData Deserialize(in JsonElement? token)
    {
        if (token is null)
            return new DesignConditionData(-1);

        return new DesignConditionData(
            token.Value.PropertyOrDefault("JobGroup"u8, -1),
            token.Value.PropertyOrDefault("Gearset"u8,  (short)-1));
    }
}
