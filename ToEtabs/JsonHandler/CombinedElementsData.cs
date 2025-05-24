using ElementsData;

namespace ToEtabs.JsonHandler
{
    public class CombinedElementsData
    {
        public List<ColumnGeometryData> Columns { get; set; }
        public List<StructuralWallData> Walls { get; set; }
        public List<BeamGeometryData> Beams { get; set; }
        public List<SlabData> Slabs { get; set; }
    }
}