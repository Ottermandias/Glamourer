using System.Collections.Generic;
using System.Linq;
using Dalamud;
using Dalamud.Plugin.Services;
using Glamourer.Structs;
using Lumina.Excel.GeneratedSheets;

namespace Glamourer;

public static class GameData
{
    private static Dictionary<byte, Job>?             _jobs;
    private static Dictionary<ushort, JobGroup>?      _jobGroups;

    public static IReadOnlyDictionary<byte, Job> Jobs(IDataManager dataManager)
    {
        if (_jobs != null)
            return _jobs;

        var sheet = dataManager.GetExcelSheet<ClassJob>()!;
        _jobs = sheet.ToDictionary(j => (byte)j.RowId, j => new Job(j));
        return _jobs;
    }

    public static IReadOnlyDictionary<ushort, JobGroup> JobGroups(IDataManager dataManager)
    {
        if (_jobGroups != null)
            return _jobGroups;

        var sheet = dataManager.GetExcelSheet<ClassJobCategory>()!;
        var jobs  = dataManager.GetExcelSheet<ClassJob>(ClientLanguage.English)!;

        static bool ValidIndex(uint idx)
        {
            if (idx is > 0 and < 36)
                return true;

            return idx switch
            {
                // Single jobs and big groups
                91  => true,
                92  => true,
                96  => true,
                98  => true,
                99  => true,
                111 => true,
                112 => true,
                129 => true,
                149 => true,
                150 => true,
                156 => true,
                157 => true,
                158 => true,
                159 => true,
                180 => true,
                181 => true,
                188 => true,
                189 => true,

                // Class + Job
                38 => true,
                41 => true,
                44 => true,
                47 => true,
                50 => true,
                53 => true,
                55 => true,
                69 => true,
                68 => true,
                93 => true,
                _   => false,
            };
        }

        _jobGroups = sheet.Where(j => ValidIndex(j.RowId))
            .ToDictionary(j => (ushort)j.RowId, j => new JobGroup(j, jobs));
        return _jobGroups;
    }
}
