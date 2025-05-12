using Autodesk.Revit.DB;

namespace FromRevit.Data
{
    public class PointData
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public static PointData FromXYZInMilli(XYZ point)
        {
            return new PointData
            {
                X = UnitUtils.ConvertFromInternalUnits(point.X, UnitTypeId.Millimeters),
                Y = UnitUtils.ConvertFromInternalUnits(point.Y, UnitTypeId.Millimeters),
                Z = UnitUtils.ConvertFromInternalUnits(point.Z, UnitTypeId.Millimeters)
            };
        }

        // Convert XYZ to PointData
        public static PointData FromXYZ(XYZ point)
        {
            return new PointData
            {
                X = point.X,
                Y = point.Y,
                Z = point.Z
            };
        }

        // Convert PointData to XYZ
        public XYZ ToXYZ()
        {
            return new XYZ(X, Y, Z);
        }

        // Implicit conversion from XYZ to PointData
        public static implicit operator PointData(XYZ point)
        {
            return FromXYZ(point);
        }

        // Implicit conversion from PointData to XYZ
        public static implicit operator XYZ(PointData pointData)
        {
            return pointData.ToXYZ();
        }
    }
}