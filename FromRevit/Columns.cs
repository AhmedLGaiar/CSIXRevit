using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FromRevit
{
    [Transaction(TransactionMode.ReadOnly)]
    public class Columns : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uIDocument = commandData.Application.ActiveUIDocument;

                Document document = uIDocument.Document;

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
