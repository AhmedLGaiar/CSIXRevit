using Autodesk.Revit.DB;
using ElementsData.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FromRevit.Utilities
{
    public class PointUtilities
    {

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



        public static PointData FromXYZInMilli(XYZ point, double thikness)
        {
            double thiknessMilli = UnitUtils.ConvertFromInternalUnits(thikness, UnitTypeId.Millimeters);

            return new PointData
            {
                X = UnitUtils.ConvertFromInternalUnits(point.X, UnitTypeId.Millimeters),
                Y = UnitUtils.ConvertFromInternalUnits(point.Y, UnitTypeId.Millimeters),
                Z = UnitUtils.ConvertFromInternalUnits(point.Z, UnitTypeId.Millimeters) + (thiknessMilli)
            };
        }


        public static PointData FromEtabs(PointData point)
        {
            return new PointData
            {
                X = point.X,
                Y = point.Y, 
                Z = point.Z,
            };
        }




    }
}
