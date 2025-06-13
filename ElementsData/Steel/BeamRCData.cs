using ElementsData.Geometry;

namespace ElementsData.Steel
{
    public class BeamRCData
    {
        public string SectionName { get; set; }
        public string uniqueName { get; set; }
        public double Width { get; set; }
        public double Depth { get; set; }
        public double Cover { get; set; }
        public int NumOfLegs { get; set; }
        public int RebarSize { get; set; }
        public int BotBars { get; set; }
        public int TopBars { get; set; }
        public int TieSize { get; set; }
        public double TieSpacingLongit { get; set; }

        public PointData StartPoint { get; set; }
        public PointData EndPoint { get; set; }
        public string Name { get; set; } // Added to match BeamGeometryData.Name
    }
}
