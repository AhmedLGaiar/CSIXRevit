using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ElementsData;
using ExportJsonFileFromRevit;
using FromRevit.Utilites;

namespace FromRevit.ElementsCommand
{
    [Transaction(TransactionMode.ReadOnly)]
    public class ShearWalls : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uIDocument = commandData.Application.ActiveUIDocument;
            Document doc = uIDocument.Document;

            try
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
                    string structuralMaterial = wallType.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)?.AsValueString() ?? "Unknown";
                    string loadBearing = wall.get_Parameter(BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT)?.AsValueString() ?? "Yes";

                    // Add structural wall data
                    structuralWallList.Add(new StructuralWallData
                    {
                        Id = wall.Id.IntegerValue.ToString(),
                        StartPoint = PointUtilites.FromXYZ(startPoint),
                        EndPoint = PointUtilites.FromXYZ(endPoint),
                        Length = wallLength,
                        Thickness = thickness,
                        Height = height,
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

                // Debug: Show how many walls were found
                if (structuralWallList.Count == 0)
                {
                    throw new Exception("No structural walls found in the model.");
                }

                // First part: Export to Desktop
                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Revit_StructuralWalls.json");
                IDataExporter<List<StructuralWallData>> exporter = new JsonDataExporter<List<StructuralWallData>>();
                exporter.Export(structuralWallList, filePath);

                // Show completion dialog
                TaskDialog.Show("Export Complete", $"Structural walls data has been exported to: \n{filePath}\nFound {structuralWallList.Count} walls.");
                TaskDialog.Show("Masar", "Thank You For Using Masar Plugin");

                return Result.Succeeded;

                // Second part: Export to Documents\masar with user-named file
                /*
                // Check for walls
                if (structuralWallList.Count == 0)
                {
                    throw new Exception("No structural walls found in the model.");
                }

                // Define the Documents folder path
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string masarPath = Path.Combine(documentsPath, "masar");

                // Create 'masar' directory if it doesn't exist
                if (!Directory.Exists(masarPath))
                {
                    Directory.CreateDirectory(masarPath);
                }

                // Prompt user to select a filename prefix
                TaskDialog dialog = new TaskDialog("Select Filename");
                dialog.MainInstruction = "Select a filename prefix for the output file (will end with 'masar')";
                dialog.CommonButtons = TaskDialogCommonButtons.Cancel;
                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Project_masar");
                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Building_masar");
                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "Structure_masar");
                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink4, "WallData_masar");

                string fileName = "";
                TaskDialogResult result = dialog.Show();
                if (result == TaskDialogResult.CommandLink1)
                {
                    fileName = "Project_masar";
                }
                else if (result == TaskDialogResult.CommandLink2)
                {
                    fileName = "Building_masar";
                }
                else if (result == TaskDialogResult.CommandLink3)
                {
                    fileName = "Structure_masar";
                }
                else if (result == TaskDialogResult.CommandLink4)
                {
                    fileName = "WallData_masar";
                }
                else
                {
                    return Result.Cancelled;
                }

                // Ensure .json extension
                if (!fileName.ToLower().EndsWith(".json"))
                {
                    fileName += ".json";
                }

                // Define full file path
                string filePath = Path.Combine(masarPath, fileName);

                // Check if file already exists and append counter if necessary
                int counter = 1;
                string baseFileName = Path.GetFileNameWithoutExtension(fileName);
                string extension = Path.GetExtension(fileName);
                while (File.Exists(filePath))
                {
                    fileName = $"{baseFileName}_{counter}{extension}";
                    filePath = Path.Combine(masarPath, fileName);
                    counter++;
                }

                // Export data
                IDataExporter<List<StructuralWallData>> exporter = new JsonDataExporter<List<StructuralWallData>>();
                exporter.Export(structuralWallList, filePath);

                // Show completion dialog with wall count
                TaskDialog.Show("Export Complete",
                    $"Structural walls data has been exported to: \n{filePath}\nFound {structuralWallList.Count} walls.");
                TaskDialog.Show("Masar", "Thank You For Using Masar Plugin");

                return Result.Succeeded;
                */
            }
            catch (Exception ex)
            {
                message = $"Error exporting structural walls: {ex.Message}\nStack Trace: {ex.StackTrace}";
                TaskDialog.Show("Error", message);
                return Result.Failed;
            }
        }
    }
}