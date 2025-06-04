using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using StructLink_X._0.Views;
using System;
using System.Reflection;

namespace StructLink_X._0.ribbon
{
    
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class StructLinkXCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Check for active Revit document
                UIDocument uiDoc = commandData.Application.ActiveUIDocument;
                if (uiDoc == null)
                {
                    message = "No active Revit document is open";
                    return Result.Failed;
                }

                Document doc = uiDoc.Document;
                if (doc == null)
                {
                    message = "Error accessing the document";
                    return Result.Failed;
                }

                // Open the main WPF window
                MainWindow mainWindow = new MainWindow(doc);
                mainWindow.ShowDialog();

                return Result.Succeeded;
            }
            catch (System.IO.FileNotFoundException ex)
            {
                message = $"File not found: {ex.FileName}\nEnsure all required files are present";
                return Result.Failed;
            }
            catch (Exception ex)
            {
                message = $"An unexpected error occurred:\n{ex.Message}\n\nAdditional details:\n{ex.StackTrace}";
                return Result.Failed;
            }
        }
    }
}