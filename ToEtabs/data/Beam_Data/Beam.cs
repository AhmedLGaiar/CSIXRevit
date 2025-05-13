using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToEtabs.Data;

namespace ToEtabs.data.Beam_Data
{
    public class Beam
    {
        public string Type { get; set; }
        public string ApplicationId { get; set; }
        public string Name { get; set; }
        public PointData StartPoint { get; set; }
        public PointData EndPoint { get; set; }
        public Material Material { get; set; }
        public Section Section { get; set; }
        public Constraints Constraints { get; set; }
    }
}
