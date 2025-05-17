using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadData
{
    public class StructuralElement
    {
        public string Id { get; set; }
        public string Type { get; set; } 
        public Dictionary<string, double> Geometry { get; set; }
        public List<LoadAssignment> Loads { get; set; }
        public Dictionary<string, double> AreaSteel { get; set; }
        public double Thickness { get; set; }
        public string Material { get; set; }
        public Dictionary<string, Dictionary<string, double>> ReinforcementByCombos { get; set; }
    }
}
