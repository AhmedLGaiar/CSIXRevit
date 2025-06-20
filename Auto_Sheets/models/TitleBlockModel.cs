using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace Auto_Sheets.Models
{
    public class TitleBlockModel
    {
        public string Name { get; set; }
        public ElementId Id { get; set; }

        public override string ToString() => Name;
    }
}
