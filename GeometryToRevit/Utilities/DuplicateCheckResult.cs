using GeometryToRevit.ExistingInfo;

namespace GeometryToRevit.Utilities
{
    public class DuplicateCheckResult
    {
        public bool HasExactDuplicate { get; set; }
        public bool HasLocationConflict { get; set; }
        public List<ExistingColumnInfo> ConflictingColumns { get; set; } = new List<ExistingColumnInfo>();
        public List<ExistingBeamInfo> ConflictingBeams { get; set; } = new List<ExistingBeamInfo>();

    }

    public enum DuplicateAction
    {
        CreateBoth,
        Replace,
        Skip
    }
}
