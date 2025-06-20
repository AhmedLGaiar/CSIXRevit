using System;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace Auto_Sheets.ExtenalApp
{
    public class ExtrnalApp : IExternalApplication
    {
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            string path = Assembly.GetExecutingAssembly().Location;

            // Create ribbon tab and panel
            try { application.CreateRibbonTab("StructLink_X"); } catch { }
            RibbonPanel panel = application.CreateRibbonPanel("Masar", "Masar Sheets");

            // Create button
            PushButtonData buttonData = new PushButtonData(
                "BTN",
                "Auto Sheets",
                path,
                "Auto_Sheets.ExtenalApp.SheetsCommand"
            );



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
    }
}
