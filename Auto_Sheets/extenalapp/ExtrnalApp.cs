using Auto_Sheets.ExtenalApp;
using Autodesk.Revit.UI;
using Nice3point.Revit.Extensions;
using System.Reflection;

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
            string tabName = "StructLink X";
            var panel = application.CreatePanel("X Sheets", tabName);

            // Get the assembly path
            string path = Assembly.GetExecutingAssembly().Location;
            PushButton button = panel.AddPushButton<SheetsCommand>("Auto Sheets");

            string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            var themedIconUri = $"/{assemblyName};component/Resources/SheetIcon.png";

            button.SetLargeImage(themedIconUri);

            return Result.Succeeded;
        }
    }
}