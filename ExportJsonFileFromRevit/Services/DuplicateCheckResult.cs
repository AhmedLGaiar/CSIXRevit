using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportJsonFileFromRevit.Services
{
    public class DuplicateCheckResult<T>
    {
        public bool HasExactDuplicate { get; set; }
        public bool HasLocationConflict { get; set; }
        public List<T> ConflictingElements { get; set; } = new List<T>();
    }

    public enum DuplicateAction
    {
        CreateBoth,
        Replace,
        Skip
    }
}
