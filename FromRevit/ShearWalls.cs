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
    public class SharedStructuralWalls : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uIDocument = commandData.Application.ActiveUIDocument;
            Document doc = uIDocument.Document;

            try

            {
                // Collect all structural walls
                var structuralWallCollector = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_WallsStructure)
                    .WhereElementIsNotElementType()
                    .Cast<Wall>()
                    .Where(w => w.WallType.Function == WallFunction.Coreshaft); //

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

                    // Calculate wall length
                    double wallLength = wallCurve.Length;

                    // Get wall thickness
                    double thickness = wall.Width;

                    // Get wall height
                    double height = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM)?.AsDouble() ??
                                    wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE)?.AsDouble() ?? 0;   //

                    // Get wall orientation vector
                    XYZ wallVector = (endPoint - startPoint).Normalize();

                    // Calculate wall orientation angle
                    double orientationAngle = Math.Atan2(wallVector.Y, wallVector.X) * (180 / Math.PI);

                    // Get levels
                    string baseLevel = wall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT)?.AsValueString() ?? "Unknown";
                    string topLevel = wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE)?.AsValueString() ?? "Unknown";

                    // Get structural wall specific information
                    string structuralMaterial = wallType.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)?.AsValueString() ?? "Unknown";
                    string loadBearing = wall.get_Parameter(BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT)?.AsValueString() ?? "Unknown";

                    // Add structural wall data
                    structuralWallList.Add(new StructuralWallData
                    {
                        Id = wall.Id.IntegerValue.ToString(),
                        StartPoint = PointData.FromXYZ(startPoint),
                        EndPoint = PointData.FromXYZ(endPoint),
                        Length = wallLength,
                        Thickness = thickness,
                        Height = height,
                        OrientationAngle = orientationAngle,
                        BaseLevel = baseLevel,
                        TopLevel = topLevel,
                        Material = structuralMaterial,
                        WallFunction = "Structural",
                        WallTypeName = wallType.Name,
                        AdditionalProperties = new Dictionary<string, string>
                        {
                            { "LoadBearing", loadBearing }
                        }
                    });
                }

                // Convert to JSON with indented formatting
                string jsonOutput = JsonConvert.SerializeObject(new { StructuralWalls = structuralWallList }, Formatting.Indented);

                // Define file path on desktop
                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Revit_StructuralWalls.json");

                // Write to file
                File.WriteAllText(filePath, jsonOutput);

                // Show completion dialog
                TaskDialog.Show("Export Complete", $"Structural walls data has been exported to: \n{filePath}");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Error exporting structural walls: {ex.Message}";
                return Result.Failed;
            }
        }
    }
}