using System;
using System.Collections.Generic;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ExportJsonFileFromRevit;

namespace FromRevit
{

    public class BeamExtractor
    {
        private readonly Document _doc;

        public BeamExtractor(Document doc)
        {
            _doc = doc;
        }

        public IEnumerable<BeamData> ExtractBeams()
        {
            var collector = new FilteredElementCollector(_doc)
                .OfClass(typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .WhereElementIsNotElementType();

            foreach (FamilyInstance beam in collector)
            {
                ElementId materialId = beam.StructuralMaterialId;
                Material material = _doc.GetElement(materialId) as Material;
                if (material == null) continue;

                LocationCurve location = beam.Location as LocationCurve;
                if (location == null) continue;

                Curve curve = location.Curve;
                XYZ startPoint = curve.GetEndPoint(0);
                XYZ endPoint = curve.GetEndPoint(1);

                double width = GetParameter(beam, "b", 0.3);
                double depth = GetParameter(beam, "h", 0.5);
                double flangeThickness = GetParameter(beam, "Flange Thickness", 0);
                double webThickness = GetParameter(beam, "Web Thickness", 0);
                double webFillet = GetParameter(beam, "Web Fillet", 0);
                double centroidHorizontal = GetParameter(beam, "Centroid Horizontal", 0);
                double centroidVertical = GetParameter(beam, "Centroid Vertical", 0);

                yield return new BeamData
                {
                    ApplicationId = beam.UniqueId,
                    Name = beam.Name,
                    StartPoint = new { x = startPoint.X, y = startPoint.Y, z = startPoint.Z },
                    EndPoint = new { x = endPoint.X, y = endPoint.Y, z = endPoint.Z },
                    Material = new { name = material.Name },
                    Section = material.Name.ToLower().Contains("steel")
                        ? (object)new
                        {
                            name = beam.Symbol.Name,
                            depth,
                            width,
                            flangeThickness,
                            webThickness,
                            webFillet,
                            centroidHorizontal,
                            centroidVertical
                        }
                        : new
                        {
                            name = beam.Symbol.Name,
                            depth,
                            width
                        },
                    Constraints = new { start = "Fixed", end = "Pinned" }
                };
            }
        }

        private double GetParameter(FamilyInstance beam, string paramName, double defaultValue)
        {
            Parameter param = beam.Symbol.LookupParameter(paramName);
            return param != null ? param.AsDouble() * 0.3048 : defaultValue;
        }
    }

    [Transaction(TransactionMode.ReadOnly)]
    public class Beams : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                BeamExtractor extractor = new BeamExtractor(doc);
                var beams = extractor.ExtractBeams().ToList();

                if (!beams.Any())
                {
                    TaskDialog.Show("Error", "No beams found in the project.");
                    return Result.Failed;
                }

                var beamsData = new
                {
                    steelBeams = beams.Where(b => ((dynamic)b.Material).name.ToLower().Contains("steel")),
                    concreteBeams = beams.Where(b => ((dynamic)b.Material).name.ToLower().Contains("concrete"))
                };

                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string filePath = Path.Combine(desktopPath, "BeamsData.json");

                IDataExporter<object> exporter = new JsonDataExporter<object>();
                exporter.Export(beamsData, filePath);

                TaskDialog.Show("Success", "Exported JSON file successfully to the desktop.");
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
