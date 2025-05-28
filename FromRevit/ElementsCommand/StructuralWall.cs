using Autodesk.Revit.DB;
using ElementsData;
using FromRevit.Utilities;

namespace FromRevit.ElementsCommand
{
    public class StructuralWall
    {
        public static List<StructuralWallData> GetShearWallData(Document doc)
        {
            var structuralWallCollector = new FilteredElementCollector(doc)
                .OfClass(typeof(Wall))
                .OfCategory(BuiltInCategory.OST_Walls)
                .Cast<Wall>();

            List<StructuralWallData> structuralWallList = new List<StructuralWallData>();

            foreach (var wall in structuralWallCollector)
            {
                // Get wall geometry
                LocationCurve locCurve = wall.Location as LocationCurve;
                if (locCurve == null) continue;

                Curve wallCurve = locCurve.Curve;
                XYZ startPoint = wallCurve.GetEndPoint(0);
                XYZ endPoint = wallCurve.GetEndPoint(1);

                // Get wall type and parameters
                WallType wallType = wall.WallType;

                // Calculate wall length (convert to feet if needed, Revit uses internal units)
                double wallLength = wallCurve.Length;

                // Get wall thickness
                double thickness = wallType.Width;

                // Get wall height
                double height = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM)?.AsDouble() ??
                                wall.get_Parameter(BuiltInParameter.WALL_STRUCTURAL_USAGE_PARAM)?.AsDouble() ?? 0;

                // Get wall orientation vector
                XYZ wallVector = (endPoint - startPoint).Normalize();

                // Calculate wall orientation angle (in degrees)
                double orientationAngle = Math.Atan2(wallVector.Y, wallVector.X) * (180 / Math.PI);

                // Get base level
                Parameter baseLevelParam = wall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT);
                if (baseLevelParam == null)
                {
                    throw new Exception($"Wall ID {wall.Id.IntegerValue} is missing WALL_BASE_CONSTRAINT parameter.");
                }

                string baseLevel = baseLevelParam.AsValueString();

                // Get top level using WALL_HEIGHT_TYPE
                Parameter topLevelIdParam = wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE);
                if (topLevelIdParam == null)
                {
                    throw new Exception($"Wall ID {wall.Id.IntegerValue} is missing WALL_HEIGHT_TYPE parameter.");
                }

                ElementId topLevelId = topLevelIdParam.AsElementId();
                Level topLevelElement = doc.GetElement(topLevelId) as Level;
                if (topLevelElement == null)
                {
                    throw new Exception($"Wall ID {wall.Id.IntegerValue} has an invalid top level ID.");
                }

                string topLevel = topLevelElement.Name;

                // Get structural wall specific information
                string structuralMaterial =
                    wallType.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)?.AsValueString() ?? "Unknown";
                string loadBearing =
                    wall.get_Parameter(BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT)?.AsValueString() ?? "Yes";

                // Add structural wall data
                structuralWallList.Add(new StructuralWallData
                {
                    Id = wall.Id.IntegerValue.ToString(),
                    StartPoint = PointUtilites.FromXYZInMilli(startPoint),
                    EndPoint = PointUtilites.FromXYZInMilli(endPoint),
                    Length = UnitUtils.ConvertFromInternalUnits(wallLength, UnitTypeId.Meters),
                    Thickness = UnitUtils.ConvertFromInternalUnits(thickness, UnitTypeId.Meters),
                    Height = UnitUtils.ConvertFromInternalUnits(height, UnitTypeId.Meters),
                    OrientationAngle = orientationAngle,
                    BaseLevel = baseLevel,
                    TopLevel = topLevel,
                    Material = structuralMaterial,
                    WallFunction = wallType.Function.ToString(),
                    WallTypeName = wallType.Name,
                    AdditionalProperties = new Dictionary<string, string>
                    {
                        { "LoadBearing", loadBearing }
                    }
                });
            }

            return structuralWallList;
        }
    }
}