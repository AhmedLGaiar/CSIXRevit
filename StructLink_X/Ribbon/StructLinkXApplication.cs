using Autodesk.Revit.UI;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace StructLink_X.Ribbon
{
    public class StructLinkXApplication : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            string tabName = "StructLink X";
            RibbonPanel panel = null;

            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch
            {
            }

            // Get or create the panel in the "Masar" tab
            panel = application.CreateRibbonPanel(tabName, "Report Exporter");

            // Path to the DLL
            string path = Assembly.GetExecutingAssembly().Location;

            // Create the PushButtonData with the correct display name
            PushButtonData buttonData = new PushButtonData(
                "StructLinkX", // Internal name
                "StructLink_X", // Display name on the ribbon
                path,
                typeof(StructLinkXCommand).FullName
            );

            // Add the button to the panel
            PushButton createButton = panel.AddItem(buttonData) as PushButton;
            createButton.ToolTip = "StructLink_X for Structural Analysis";

            // Load and set the icon
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = "StructLink_X.resources.icon.png"; // Adjust based on your namespace and folder structure
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    createButton.LargeImage = bitmap; // 32x32 icon
                    createButton.Image = bitmap;      // 16x16 icon (optional)
                }
                else
                {
                    // Optional: Log or debug if the resource is not found
                    System.Diagnostics.Debug.WriteLine($"Icon resource not found: {resourceName}");
                }
            }

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}