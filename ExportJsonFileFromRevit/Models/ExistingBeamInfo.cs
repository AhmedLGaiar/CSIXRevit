using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportJsonFileFromRevit.Models
{
    public class ExistingBeamInfo
    {
        public FamilyInstance BeamInstance { get; set; }
        public XYZ StartPoint { get; set; }
        public XYZ EndPoint { get; set; }
        public double Width { get; set; }
        public double Depth { get; set; }
        public string TypeName { get; set; }
    }

}
