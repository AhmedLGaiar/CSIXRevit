using ElementsData;

namespace ToEtabs.JsonHandler
{
    public class CombinedElementsData
    {
        public List<ColumnData> Columns { get; set; }
        public List<StructuralWallData> Walls { get; set; }
        public List<BeamData> Beams { get; set; }
        public List<SlabData> Slabs { get; set; }
    }
}