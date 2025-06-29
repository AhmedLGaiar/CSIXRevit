using ElementsData.Geometry;

namespace EtabsGeometryExport.Model
{
    public class ETABSStructuralData
    {
        public List<BeamGeometryData> Beams { get; set; } = new List<BeamGeometryData>();
        public List<ColumnGeometryData> Columns { get; set; } = new List<ColumnGeometryData>();
        public List<SlabData> Slabs { get; set; } = new List<SlabData>();

        public List<StructuralWallData> StructWalls { get; set; }= new List<StructuralWallData>();

        public DateTime ExtractionDate { get; set; } = DateTime.Now;

        public int TotalElementCount => Beams.Count + Columns.Count+ Slabs.Count + StructWalls.Count;
    }

}
