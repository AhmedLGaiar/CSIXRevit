using Autodesk.Revit.DB;
using FromRevit.Data.Beam_data;

namespace FromRevit.Data
{
    public class BeamData
    {
        public string Type { get; set; } = "Beam";
        public string ApplicationId { get; set; }
        public string Name { get; set; }
        public PointData StartPoint { get; set; }
        public PointData EndPoint { get; set; }
        public MaterialData Material { get; set; }
        public SectionData Section { get; set; }
        public object Constraints { get; set; }
    }
}
