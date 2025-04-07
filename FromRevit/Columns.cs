using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using FromRevit.Data;
using FromRevit.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FromRevit
{
    [Transaction(TransactionMode.ReadOnly)]
    public class Columns : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uIDocument = commandData.Application.ActiveUIDocument;
            Document doc = uIDocument.Document;

            try
            {

                IEnumerable<FamilyInstance> colCollector = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance))
                                                        .OfCategory(BuiltInCategory.OST_StructuralColumns)
                                                        .Cast<FamilyInstance>();

                List<ColumnData> columnList = new List<ColumnData>();

                foreach (var col in colCollector)
                {


                    // Get base and top points
                    LocationPoint loc = col.Location as LocationPoint;
                    XYZ basePoint = loc.Point;
                    double height = col.get_Parameter(BuiltInParameter.INSTANCE_LENGTH_PARAM).AsDouble();
                    XYZ topPoint = basePoint + new XYZ(0, 0, height);

                    // Get the column type
                    ElementId typeId = col.GetTypeId();
                    ElementType colType = doc.GetElement(typeId) as ElementType;

                    // Get width and length from parameters
                    double width = colType?.LookupParameter("b")?.AsDouble() ?? 0;
                    double length = colType?.LookupParameter("h")?.AsDouble() ?? 0;

                    // Calculate slanted angle for non-vertical columns
                    double slantedAngle = 0.0;
                    if (!Math.Abs(topPoint.X - basePoint.X).IsAlmostZero() ||
                        !Math.Abs(topPoint.Y - basePoint.Y).IsAlmostZero())
                    {
                        XYZ vector = topPoint - basePoint;
                        slantedAngle = Math.Atan2(Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y), vector.Z) * (180 / Math.PI);
                    }

                    // Get section name
                    string sectionName = colType?.Name ?? "Unknown";

                    // Get material
                    Parameter materialParam = colType?.LookupParameter("Material");
                    string material = materialParam != null ? materialParam.AsValueString() : "Unknown";

                    // Get rotation
                    double rotation = loc.Rotation * (180 / Math.PI); // Convert to degrees

                    // Get base and top levels
                    string baseLevel = col.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM).AsValueString();
                    string topLevel = col.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM).AsValueString();

                    // Get actual fixity conditions
                    Fixity fixity = Fixity.GetColumnFixity(col);

                    // Add column data
                    columnList.Add(new ColumnData
                    {
                        Id = col.Id.IntegerValue.ToString(),
                        BasePoint = PointData.FromXYZ(basePoint),
                        TopPoint = PointData.FromXYZ(topPoint),
                        Width = width,
                        Length = length,
                        SectionName = sectionName,
                        Material = material,
                        Rotation = rotation,
                        SlantedAngle = slantedAngle,
                        BaseLevel = baseLevel,
                        TopLevel = topLevel,
                        Story = baseLevel,
                        Fixity = fixity
                    });
                }

                // Convert to JSON
                string jsonOutput = JsonConvert.SerializeObject(new { Columns = columnList }, Formatting.Indented);

                // Define file path
                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Revit_Columns.json");

                // Write to file
                File.WriteAllText(filePath, jsonOutput);

                TaskDialog.Show("Export Complete", $"Column data has been exported to: \n{filePath}");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
