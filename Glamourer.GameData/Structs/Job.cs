using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets;

namespace Glamourer.Structs;

// A struct containing the different jobs the game supports.
// Also contains the jobs Name and Abbreviation as strings.
public readonly struct Job
{
    public readonly string   Name;
    public readonly string   Abbreviation;
    public readonly ClassJob Base;

    public uint Id
        => Base.RowId;

    public JobFlag Flag
        => (JobFlag)(1u << (int)Base.RowId);

    public Job(ClassJob job)
    {
        Base         = job;
        Name         = job.Name.ToDalamudString().ToString();
        Abbreviation = job.Abbreviation.ToDalamudString().ToString();
    }

    public override string ToString()
        => Name;
}
