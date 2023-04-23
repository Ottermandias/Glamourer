using System.Collections.Generic;
using Dalamud.Data;

namespace Glamourer.Services;

public class JobService
{
    public readonly IReadOnlyDictionary<byte, Structs.Job>        Jobs;
    public readonly IReadOnlyDictionary<ushort, Structs.JobGroup> JobGroups;

    public JobService(DataManager gameData)
    {
        Jobs      = GameData.Jobs(gameData);
        JobGroups = GameData.JobGroups(gameData);
    }
}
