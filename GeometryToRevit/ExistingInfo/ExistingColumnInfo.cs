using Autodesk.Revit.DB;

namespace GeometryToRevit.ExistingInfo
{
    public class ExistingColumnInfo
    {
        public FamilyInstance ColumnInstance { get; set; }
        public XYZ BasePoint { get; set; }
        public XYZ TopPoint { get; set; }
        public double Width { get; set; }
        public double Depth { get; set; }
        public string TypeName { get; set; }
    }
}