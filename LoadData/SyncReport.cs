using System;
using System.Collections.Generic;
using System.Linq;

namespace LoadData

{
    public class SyncReport
    {
        public int NewLoads { get; set; }
        public int ModifiedLoads { get; set; }
        public int DeletedLoads { get; set; }
        public int UnchangedLoads { get; set; }
        public int TotalLoads { get; set; }

        public override string ToString()
        {
            return $"Synchronization Report:\n" +
                   $"- New loads: {NewLoads}\n" +
                   $"- Modified loads: {ModifiedLoads}\n" +
                   $"- Deleted loads: {DeletedLoads}\n" +
                   $"- Unchanged loads: {UnchangedLoads}\n" +
                   $"- Total loads: {TotalLoads}";
        }
    }
}
