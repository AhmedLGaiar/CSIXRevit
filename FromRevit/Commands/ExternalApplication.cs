using Autodesk.Revit.UI;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

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
            PushButtonData exportButtonData = new PushButtonData(
                "Exporter",
                "Export To Etabs",
                path,
                typeof(ExportFromRevit).FullName
            );

            PushButton exportButton = panel.AddItem(exportButtonData) as PushButton;
            string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            var themedIconUri = $"/{assemblyName};component/Resources/Icons/concreteBuilding.png";

            exportButton.LargeImage = PushImage("FromRevit.Resources.Icons.concerteBuilding.png");

            // Add the button to the panel
            if (exportButton == null)
            {
                TaskDialog.Show("Error", "Failed to create PushButton.");
                return Result.Failed;
            }

            // Set tooltip
            exportButton.ToolTip = "Export Structural Elements Geometry To Etabs From Revit";

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
        private BitmapImage PushImage(string resourcePath)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            {
                if (stream == null)
                    throw new Exception($"Image not found: {resourcePath}");

                var image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = stream;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                return image;
            }

        }
    }
}