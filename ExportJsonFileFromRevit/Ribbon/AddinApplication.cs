using Autodesk.Revit.UI;
using System.IO;
using System.Reflection;
using System.Windows.Controls.Ribbon;
using System.Windows.Media.Imaging;

namespace ExportJsonFileFromRevit.Ribbon
{
    public class AddinApplication : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Check if the "Masr" tab already exists
                string tabName = "Masar";
                bool tabExists = false;
                foreach (string existingTabName in application.GetRibbonPanels().Select(panel => panel.Name))
                {
                    if (existingTabName == tabName)
                    {
                        tabExists = true;
                        break;
                    }
                }

                // Create the "Masr" tab only if it doesn't exist
                if (!tabExists)
                {
                    application.CreateRibbonTab(tabName);
                }

                // Create a ribbon panel
                RibbonPanel panel = application.CreateRibbonPanel(tabName, "JSON Importer");

                // Get the assembly path
                string assemblyPath = Assembly.GetExecutingAssembly().Location;

                // Create push button data
                PushButtonData buttonData = new PushButtonData(
                    "JsonImporterCommand",
                    "Import JSON",
                    assemblyPath,
                    "ExportJsonFileFromRevit.Ribbon.AddinCommand")
                {
                    ToolTip = "Import beams and columns from a JSON file.",
                    LongDescription = "Select a JSON file to create beams and columns in the Revit model."
                };

                // Optionally, set an icon (place 16x16 and 32x32 PNG images in the same folder as the DLL)
                string iconPath = Path.Combine(Path.GetDirectoryName(assemblyPath), "icon.png");
                if (File.Exists(iconPath))
                {
                    buttonData.LargeImage = new BitmapImage(new Uri(iconPath));
                }

                // Add the button to the panel
                panel.AddItem(buttonData);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Failed to create ribbon: {ex.Message}");
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}