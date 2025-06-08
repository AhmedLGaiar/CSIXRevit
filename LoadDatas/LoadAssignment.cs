using System;

namespace CSIXRevit.LoadData
{
    public class LoadAssignment
    {
        public string ElementID { get; set; }
        public string LoadPattern { get; set; }
        public string LoadType { get; set; }
        public double Value { get; set; }
        public string Unit { get; set; }
        public int Dir { get; set; }
        
        public double? StartDistance { get; set; }
        public double? EndDistance { get; set; }
        public string RelativeDistance { get; set; }
        
        public string UniqueIdentifier { get; set; }
        public string SourcePlatform { get; set; }
        public DateTime LastModified { get; set; }
        public LoadSyncState SyncState { get; set; }
        
        public void GenerateUniqueIdentifier()
        {
            if (string.IsNullOrEmpty(UniqueIdentifier))
            {
                UniqueIdentifier = $"{SourcePlatform}_{ElementID}_{LoadType}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            }
        }
        
        public bool HasSameValues(LoadAssignment other)
        {
            if (other == null)
                return false;
                
            return this.LoadPattern == other.LoadPattern &&
                   this.LoadType == other.LoadType &&
                   Math.Abs(this.Value - other.Value) < 0.0001 &&
                   this.Unit == other.Unit &&
                   this.Dir == other.Dir &&
                   this.StartDistance == other.StartDistance &&
                   this.EndDistance == other.EndDistance &&
                   this.RelativeDistance == other.RelativeDistance;
        }
    }
    
    public enum LoadSyncState
    {
        New,
        Modified,
        Unchanged,
        Deleted
    }
}
