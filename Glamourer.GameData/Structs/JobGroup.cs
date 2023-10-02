using System;
using System.Diagnostics;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace Glamourer.Structs;

[Flags]
public enum JobFlag : ulong
{ }

// The game specifies different job groups that can contain specific jobs or not.
public readonly struct JobGroup
{
    public readonly  string  Name;
    public readonly  int     Count;
    public readonly  uint    Id;
    private readonly JobFlag _flags;

    // Create a job group from a given category and the ClassJob sheet.
    // It looks up the different jobs contained in the category and sets the flags appropriately.
    public JobGroup(ClassJobCategory group, ExcelSheet<ClassJob> jobs)
    {
        Count  = 0;
        _flags = 0ul;
        Id     = group.RowId;
        Name   = group.Name.ToString();

        Debug.Assert(jobs.RowCount < 64, $"Number of Jobs exceeded 63 ({jobs.RowCount}).");
        foreach (var job in jobs)
        {
            var abbr = job.Abbreviation.ToString();
            if (abbr.Length == 0)
                continue;

            var prop = group.GetType().GetProperty(abbr);
            Debug.Assert(prop != null, $"Could not get job abbreviation {abbr} property.");

            if (!(bool)prop.GetValue(group)!)
                continue;

            ++Count;
            _flags |= (JobFlag)(1ul << (int)job.RowId);
        }
    }

    // Check if a job is contained inside this group.
    public bool Fits(Job job)
        => _flags.HasFlag(job.Flag);

    // Check if any of the jobs in the given flags fit this group.
    public bool Fits(JobFlag flag)
        => (_flags & flag) != 0;

    // Check if a job is contained inside this group.
    public bool Fits(uint jobId)
    {
        var flag = (JobFlag)(1ul << (int)jobId);
        return _flags.HasFlag(flag);
    }
}
