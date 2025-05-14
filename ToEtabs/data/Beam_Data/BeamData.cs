using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using ToEtabs.Data;

namespace ToEtabs.data.Beam_Data
{
    public class BeamData
    {
        public string Type { get; set; } = "Beam";
        public string ApplicationId { get; set; }
        public string Name { get; set; }
        public PointData StartPoint { get; set; }
        public PointData EndPoint { get; set; }
        public MaterialData Material { get; set; }
        public SectionData Section { get; set; }
        public object Constraints { get; set; }
    }
}
