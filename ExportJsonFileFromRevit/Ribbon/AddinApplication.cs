using Autodesk.Revit.UI;
using Nice3point.Revit.Extensions;
using System.Reflection;

namespace ExportJsonFileFromRevit.Ribbon
{
    public class AddinApplication : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            string tabName = "StructLink X";
            var panel = application.CreatePanel("X Sheets", tabName);

            // Get the assembly path
            string path = Assembly.GetExecutingAssembly().Location;
            PushButton button = panel.AddPushButton<AddinCommand>("Json Importer");

            //string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            //var themedIconUri = $"/{assemblyName};component/Resources/Icons/concerteBuilding.png";

            //button.SetLargeImage(themedIconUri);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}