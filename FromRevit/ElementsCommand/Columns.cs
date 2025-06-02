using Autodesk.Revit.DB;
using FromRevit.Helpers;
using FromRevit.Utilities;
using ElementsData.Geometry;

namespace FromRevit.ElementsCommand
{
    public class Columns
    {
        public static List<ColumnGeometryData> GetColumnGeometryData(Document doc)
        {
            IEnumerable<FamilyInstance> colCollector = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_StructuralColumns)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>();

            List<ColumnGeometryData> columnList = new List<ColumnGeometryData>();

            foreach (var col in colCollector)
            {
                // Get base and top points
                LocationPoint loc = col.Location as LocationPoint;
                double height = col.get_Parameter(BuiltInParameter.INSTANCE_LENGTH_PARAM).AsDouble();
                ElementId baseLevelId = col.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM).AsElementId();
                Level baseLvl = doc.GetElement(baseLevelId) as Level;
                double baseZ = baseLvl?.Elevation ?? 0;

                XYZ basePoint = new XYZ(loc.Point.X, loc.Point.Y, baseZ);
                XYZ topPoint = new XYZ(loc.Point.X, loc.Point.Y, baseZ + height);


                // Get the column type
                ElementId typeId = col.GetTypeId();
                ElementType colType = doc.GetElement(typeId) as ElementType;

                // Get width and length from parameters
                double width = colType?.LookupParameter("b")?.AsDouble() ?? 0;
                double depth = colType?.LookupParameter("h")?.AsDouble() ?? 0;

                // Calculate slanted angle for non-vertical columns
                double slantedAngle = 0.0;
                if (!Math.Abs(topPoint.X - basePoint.X).IsAlmostZero() ||
                    !Math.Abs(topPoint.Y - basePoint.Y).IsAlmostZero())
                {
                    XYZ vector = topPoint - basePoint;
                    slantedAngle = Math.Atan2(Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y), vector.Z) *
                                   (180 / Math.PI);
                }

                // Get section name
                string sectionName = colType?.Name ?? "Unknown";

                // Get material
                ElementId materialId = col.StructuralMaterialId;
                Element materialElement = doc.GetElement(materialId);

                string material = materialElement.Name;

                // Get rotation
                double rotation = loc.Rotation * (180 / Math.PI); // Convert to degrees

                // Get base and top levels
                string baseLevel = col.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM).AsValueString();
                string topLevel = col.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM).AsValueString();

                // Add column data
                columnList.Add(new ColumnGeometryData
                {
                    Id = col.Id.IntegerValue.ToString(),
                    BasePoint = PointUtilities.FromXYZInMilli(basePoint),
                    TopPoint = PointUtilities.FromXYZInMilli(topPoint),
                    Width = UnitUtils.ConvertFromInternalUnits(width, UnitTypeId.Meters),
                    Depth = UnitUtils.ConvertFromInternalUnits(depth, UnitTypeId.Meters),
                    SectionName = sectionName,
                    Rotation = rotation,
                    SlantedAngle = slantedAngle,
                    BaseLevel = baseLevel,
                    TopLevel = topLevel,
                    Story = baseLevel,
                });
            }

            return columnList;
        }
    }
}