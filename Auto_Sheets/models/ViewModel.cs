using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace Auto_Sheets.Models
{
    public class ViewModel
    {
        public string ViewName { get; set; }
        public string ViewType { get; set; }
        public ElementId ViewId { get; set; }

        public string DisplayName => $"{ViewType} : {ViewName}";
    }
}
