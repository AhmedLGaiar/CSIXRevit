using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementsData
{
    public class SlabData
    {
        public List<PointData> OuterBoundary { get; set; }
        public List<List<PointData>> Openings { get; set; } = new List<List<PointData>>();
        public double Thickness { get; set; }
        public string Level { get; set; }
        public string SectionName { get; set; }
    }
}
