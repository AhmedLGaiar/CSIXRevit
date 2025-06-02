using Autodesk.Revit.DB;

namespace GeometryToRevit.ExistingInfo
{
    public class ExistingBeamInfo
    {
        public FamilyInstance BeamInstance { get; set; }
        public XYZ StartPoint { get; set; }
        public XYZ EndPoint { get; set; }
        public double Width { get; set; }
        public double Depth { get; set; }
        public string TypeName { get; set; }
    }
}
