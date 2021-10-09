using System;
using System.Diagnostics;
using System.Linq;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace Glamourer
{
    public readonly struct JobGroup
    {
        public readonly  string Name;
        private readonly ulong  _flags;
        public readonly  int    Count;
        public readonly  uint   Id;

        public JobGroup(ClassJobCategory group, ExcelSheet<ClassJob> jobs)
        {
            Count  = 0;
            _flags = 0ul;
            Id     = group.RowId;
            Name   = group.Name.ToString();

            Debug.Assert(jobs.RowCount < 64);
            foreach (var job in jobs)
            {
                var abbr = job.Abbreviation.ToString();
                if (!abbr.Any())
                    continue;

                var prop = group.GetType().GetProperty(abbr);
                Debug.Assert(prop != null);

                if (!(bool) prop.GetValue(group)!)
                    continue;

                ++Count;
                _flags |= 1ul << (int) job.RowId;
            }
        }

        public bool Fits(Job job)
            => Fits(job.Id);

        public bool Fits(uint jobId)
        {
            var flag = 1ul << (int)jobId;
            return (flag & _flags) != 0;
        }
    }
}
