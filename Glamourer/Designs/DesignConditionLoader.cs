using Dalamud.Interface.ImGuiNotification;
using Glamourer.Interop;
using Luna;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Structs;

namespace Glamourer.Designs;

public class DesignConditionsLoader(JobService jobs) : IService
{
    public DesignConditions AlwaysTrue
        => new(jobs.JobGroups[1]);

    public DesignConditions Canonicalize(DesignConditions conditions)
        => conditions.Constant is true ? AlwaysTrue : conditions;

    public bool TryParse(JToken? token, out DesignConditions conditions, string? descriptionPrefix, string description)
    {
        var data    = DesignConditionData.Deserialize(token);
        var success = TryConvert(data, out conditions);
        if (!success && descriptionPrefix is not null)
            Glamourer.Messager.NotificationMessage(
                $"Error parsing {descriptionPrefix} {description}: The job condition {data.JobGroupId} does not exist.",
                NotificationType.Warning);

        return success;
    }

    public bool TryConvert(DesignConditionData data, out DesignConditions conditions)
    {
        if (data.JobGroupId < 0)
        {
            conditions = new DesignConditions(jobs.JobGroups[1], data.GearsetIndex);
            return true;
        }

        if (jobs.JobGroups.TryGetValue((JobGroupId)data.JobGroupId, out var jobGroup))
        {
            conditions = new DesignConditions(jobGroup, data.GearsetIndex);
            return true;
        }

        conditions = new DesignConditions(default, data.GearsetIndex);
        return false;
    }
}
