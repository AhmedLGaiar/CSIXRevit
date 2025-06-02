namespace ElementsData.Geometry
{
    public class BeamGeometryData
    {
        public string ApplicationId { get; set; }
        public string Name { get; set; }
        public PointData StartPoint { get; set; }
        public PointData EndPoint { get; set; }
        public double Width { get; set; }
        public double Depth { get; set; }
    }
}
