using Lumina.Excel.GeneratedSheets;

namespace Glamourer
{
    public readonly struct Job
    {
        public readonly string   Name;
        public readonly string   Abbreviation;
        public readonly ClassJob Base;

        public uint Id
            => Base.RowId;

        public Job(ClassJob job)
        {
            Base         = job;
            Name         = job.Name.ToString();
            Abbreviation = job.Abbreviation.ToString();
        }
    }
}
