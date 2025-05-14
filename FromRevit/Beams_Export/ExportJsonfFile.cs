using System;
using System.IO;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ExportJsonFileFromRevit;

namespace FromRevit
{
    [Transaction(TransactionMode.ReadOnly)]
    public class ExportJsonfFile : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                BeamExtractor extractor = new BeamExtractor(doc);
                var concreteBeams = extractor.ExtractConcreteBeams().ToList();

                if (!concreteBeams.Any())
                {
                    TaskDialog.Show("Error", "No concrete beams found in the project.");
                    return Result.Failed;
                }

                var beamsData = new
                {
                    concreteBeams = concreteBeams
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
