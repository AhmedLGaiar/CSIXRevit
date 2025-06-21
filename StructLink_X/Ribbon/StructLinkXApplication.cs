using Autodesk.Revit.UI;
using Nice3point.Revit.Extensions;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace StructLink_X.Ribbon
{
    public class StructLinkXApplication : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            string tabName = "StructLink X";
            var panel = application.CreatePanel("Report Generator", tabName);

            // Get the assembly path
            string path = Assembly.GetExecutingAssembly().Location;
            PushButton button = panel.AddPushButton<StructLinkXCommand>("Export Design Report");

            string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            var themedIconUri = $"/{assemblyName};component/Resources/report_16939689.png";

            button.SetLargeImage(themedIconUri);

            return Result.Succeeded;
           
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}