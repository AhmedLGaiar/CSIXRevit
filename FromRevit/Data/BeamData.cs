namespace FromRevit.Data
{
    public class BeamData
    {
        public string Type { get; set; } = "Beam";
        public string ApplicationId { get; set; }
        public string Name { get; set; }
        public object StartPoint { get; set; }
        public object EndPoint { get; set; }
        public object Material { get; set; }
        public object Section { get; set; }
        public object Constraints { get; set; }
    }
}
