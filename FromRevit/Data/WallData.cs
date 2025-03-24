using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FromRevit.Data
{
    public class WallData
    {
        public string Id { get; set; }
        public PointData StartPoint { get; set; }
        public PointData EndPoint { get; set; }
        public double Length { get; set; }
        public double Thickness { get; set; }
        public double Height { get; set; }
        public double OrientationAngle { get; set; }
        public string BaseLevel { get; set; }
        public string TopLevel { get; set; }
        public string Material { get; set; }
        public string WallFunction { get; set; }
        public string WallTypeName { get; set; }
    }
}
