using Autodesk.Revit.UI;
using System.Reflection;

namespace FromRevit.Commands
{
    public class ExternalApplication : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            // Create Tab
            string tabName = "StructLink X";
            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch (Exception)
            {
                // Tab may already exist – safe to ignore
            }

            // Create Panel
            RibbonPanel panel = application.CreateRibbonPanel(tabName, "Exporter");

            // Path to the DLL
            string path = Assembly.GetExecutingAssembly().Location;

            // Tag Button
            PushButtonData buttonData = new PushButtonData(
                "Exporter",
                "Export To Etabs",
                path,
                typeof(ExportFromRevit).FullName
            );

            PushButton createButton = panel.AddItem(buttonData) as PushButton;
            createButton.ToolTip = "Export Structural Elements Geometry To Etabs From Revit";

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}