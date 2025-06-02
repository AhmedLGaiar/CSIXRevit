namespace ElementsData.Geometry
{
    public class ColumnGeometryData
    {
        public string Id { get; set; }
        public PointData BasePoint { get; set; }
        public PointData TopPoint { get; set; } 
        public double Width { get; set; }
        public double Depth { get; set; }
        public string SectionName { get; set; }
        public double Rotation { get; set; }
        public double SlantedAngle { get; set; }
        public string BaseLevel { get; set; }
        public string TopLevel { get; set; }
        public string Story { get; set; }
    }
}
