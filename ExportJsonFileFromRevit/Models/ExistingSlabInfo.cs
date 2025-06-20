using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportJsonFileFromRevit.Models
{
    public class ExistingSlabInfo
    {
        public Floor SlabInstance { get; set; }
        public List<XYZ> BoundaryPoints { get; set; }
        public List<XYZ> FilteredBoundaryPoints { get; set; }
        public List<XYZ> SimplifiedBoundaryPoints { get; set; }
        public XYZ Centroid { get; set; }
        public XYZ FilteredCentroid { get; set; }
        public XYZ SimplifiedCentroid { get; set; }
        public double Thickness { get; set; }
        public string TypeName { get; set; }
        public string LevelName { get; set; }
        public double Elevation { get; set; }
        public double Area { get; set; }
        public BoundingBoxXYZ BoundingBox { get; set; }
    }
}
