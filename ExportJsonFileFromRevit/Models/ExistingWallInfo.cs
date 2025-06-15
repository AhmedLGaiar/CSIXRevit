using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportJsonFileFromRevit.Models
{
    public class ExistingWallInfo
    {
        public Wall WallInstance { get; set; }
        public XYZ StartPoint { get; set; }
        public XYZ EndPoint { get; set; }
        public double Thickness { get; set; } // in mm
        public double Height { get; set; }    // in mm
        public string TypeName { get; set; }
        public string WallName { get; set; }
    }
}
