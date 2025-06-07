using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using StructLink_X._0.Views;
using System;
using System.IO;
using System.Reflection;

namespace StructLink_X._0.ribbon


/* 
 * 
 
 ازيك يا جوجو معلش هتعبك معايا 
   

    لما تيجي تبداء تصلحه ابعتلي ندخل ميتنج
 
 
 
 
 كل الحب --- للوولوووووووووو
 
 
 
 */
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

                AppDomain.CurrentDomain.AssemblyResolve += ResolveStyleLibrary;
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
        private static Assembly ResolveStyleLibrary(object sender, ResolveEventArgs args)
        {
            var requestedAssembly = new AssemblyName(args.Name).Name + ".dll";
            var pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var fullPath = Path.Combine(pluginDir, requestedAssembly);

            return File.Exists(fullPath) ? Assembly.LoadFrom(fullPath) : null;
        }
    }
}