using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ExportJsonFileFromRevit.Views;
using System.IO;
using System.Reflection;

namespace ExportJsonFileFromRevit.Ribbon
{
    [Transaction(TransactionMode.Manual)]
    public class AddinCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var doc = commandData.Application.ActiveUIDocument.Document;
                AppDomain.CurrentDomain.AssemblyResolve += ResolveStyleLibrary;


                // Show the WPF Window directly
                MainWindow window = new MainWindow(doc);
                window.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
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