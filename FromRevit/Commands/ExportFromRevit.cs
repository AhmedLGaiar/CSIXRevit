using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using FromRevit.ViewModels;
using FromRevit.Views;

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
    }
}
