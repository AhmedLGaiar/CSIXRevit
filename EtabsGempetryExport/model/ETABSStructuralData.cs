using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ElementsData;
using ElementsData.Geometry;
using ElementsData.Steel;
using EtabsGempetryExport.Model.HelperClasses;
using ETABSv1;
using Microsoft.Win32;

namespace EtabsGempetryExport.Model
{
    public class ETABSStructuralData
    {
        public List<BeamGeometryData> Beams { get; set; } = new List<BeamGeometryData>();
        public List<ColumnGeometryData> Columns { get; set; } = new List<ColumnGeometryData>();
        public List<SlabData> Slabs { get; set; } = new List<SlabData>();

        public List<StructuralWallData> StructWalls { get; set; }= new List<StructuralWallData>();

        public DateTime ExtractionDate { get; set; } = DateTime.Now;

        public int TotalElementCount => Beams.Count + Columns.Count+ Slabs.Count + StructWalls.Count;
    }




 }
