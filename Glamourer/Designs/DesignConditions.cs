using Glamourer.Automation;
using ImSharp;
using Newtonsoft.Json.Linq;
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
    public JObject Serialize()
        => new JObject
        {
            ["Gearset"]  = GearsetIndex,
            ["JobGroup"] = JobGroupId,
        };

    public static DesignConditionData Deserialize(JToken? token)
    {
        if (token is null)
            return new DesignConditionData(-1);

        return new DesignConditionData(
            token["JobGroup"]?.ToObject<int>() ?? -1,
            token["Gearset"]?.ToObject<short>() ?? -1);
    }
}
