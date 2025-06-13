using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportJsonFileFromRevit.Models
{

    public class ExistingColumnInfo
    {
        public FamilyInstance ColumnInstance { get; set; }
        public XYZ BasePoint { get; set; }
        public XYZ TopPoint { get; set; }
        public double Width { get; set; }
        public double Depth { get; set; }
        public string TypeName { get; set; }
    }
}
