using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using FromRevit.ViewModels;
using FromRevit.Views;
using System.Reflection;

namespace FromRevit.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class ExportFromRevit : IExternalCommand
    {
        public static Document document;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uIDocument = commandData.Application.ActiveUIDocument;
            document = uIDocument.Document;
            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += ResolveStyleLibrary;

                MainView sheetView = new MainView(new MainViewViewModel());
                sheetView.ShowDialog();
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
