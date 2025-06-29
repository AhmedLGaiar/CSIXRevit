using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Auto_Sheets.Views;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Auto_Sheets.ExtenalApp
{
    [Transaction(TransactionMode.Manual)]
    public class SheetsCommand : IExternalCommand
    {
        public static Document document { get; set; }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uIDocument = commandData.Application.ActiveUIDocument;

                document = uIDocument.Document;

                MainWindow auto_Sheets = new MainWindow(uIDocument);

                auto_Sheets.ShowDialog();


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
