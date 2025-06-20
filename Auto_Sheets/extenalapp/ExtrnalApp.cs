using System;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace Auto_Sheets.ExternalApp
{
    public class ExternalApp : IExternalApplication
    {
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Check if the "StructLink X" tab already exists
                string tabName = "StructLink X";
                var ribbonPanels = application.GetRibbonPanels(tabName); // Use GetRibbonPanels instead of GetRibbonTabs
                bool tabExists = ribbonPanels != null && ribbonPanels.Any();

                // Create the "StructLink X" tab only if it doesn't exist
                if (!tabExists)
                {
                    application.CreateRibbonTab(tabName);
                }

                // Create a ribbon panel
                RibbonPanel panel = application.CreateRibbonPanel(tabName, "Masar Sheets");

                // Get the assembly path
                string path = Assembly.GetExecutingAssembly().Location;

                // Create button
                PushButtonData buttonData = new PushButtonData(
                    "BTN",
                    "Auto Sheets",
                    path,
                    "Auto_Sheets.ExternalApp.SheetsCommand")
                {
                    ToolTip = "Create sheets automatically in Revit.",
                    LongDescription = "Automate the creation of sheets in the Revit model."
                };

                // Add the button to the panel
                PushButton pushButton = panel.AddItem(buttonData) as PushButton;

                // Load embedded PNG icon
                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = "Auto_Sheets.Resources.SheetIcon.png";

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        BitmapImage image = new BitmapImage();
                        image.BeginInit();
                        image.StreamSource = stream;
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.EndInit();
                        pushButton.LargeImage = image;
                    }
                    else
                    {
                        TaskDialog.Show("Error", $"Resource '{resourceName}' not found.");
                    }
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Failed to create ribbon: {ex.Message}");
                return Result.Failed;
            }
        }
    }
}