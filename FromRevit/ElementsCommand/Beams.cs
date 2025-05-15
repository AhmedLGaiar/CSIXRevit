using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public class Beams : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;
            try
            {
                FilteredElementCollector Beams = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilyInstance))
                    .OfCategory(BuiltInCategory.OST_StructuralFraming)
                    .WhereElementIsNotElementType();

                List<BeamData> BeamList = new List<BeamData>();

                foreach (FamilyInstance beam in Beams)
                {
                    ElementId materialId = beam.StructuralMaterialId;
                    Material material = doc.GetElement(materialId) as Material;
                    if (material == null) continue;

                    if (!material.Name.ToLower().Contains("concrete"))
                        continue;

                    LocationCurve location = beam.Location as LocationCurve;
                    if (location == null) continue;

                    Curve curve = location.Curve;
                    XYZ startPoint = curve.GetEndPoint(0);
                    XYZ endPoint = curve.GetEndPoint(1);

                    double width = BeamUtilities.GetParameter(beam, "b", 0.3);
                    double depth = BeamUtilities.GetParameter(beam, "h", 0.5);

                    string BeamName = beam.Name;
                    string BeamId = beam.UniqueId;

                    BeamList.Add(new BeamData
                    {
                        ApplicationId = BeamId,
                        Name = BeamName,
                        StartPoint = PointUtilites.FromXYZInMilli(startPoint),
                        EndPoint = PointUtilites.FromXYZInMilli(endPoint),
                        Depth = UnitUtils.ConvertFromInternalUnits(depth, UnitTypeId.Meters),
                        Width = UnitUtils.ConvertFromInternalUnits(width, UnitTypeId.Meters),
                    });
                }

                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "BeamsData.json");

                IDataExporter<List<BeamData>> exporter = new JsonDataExporter<List<BeamData>>();
                exporter.Export(BeamList, filePath);

                TaskDialog.Show("Export Complete", $"Beams data has been exported to: \n{filePath}");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"An error occurred: {ex.Message}");
                return Result.Failed;
            }
        }
    }
}