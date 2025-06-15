using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementsData.Geometry
{
    public class WallRCData
    {
        public string Id { get; set; }
        public PointData StartPoint { get; set; } // Start point in meters
        public PointData EndPoint { get; set; }   // End point in meters
        public double Length { get; set; }        // Length in meters
        public double Thickness { get; set; }     // Thickness in meters
        public double Height { get; set; }        // Height in meters
        public double OrientationAngle { get; set; } // Angle in radians
        public string BaseLevel { get; set; }
        public string TopLevel { get; set; }
        public string Material { get; set; }
        public string WallFunction { get; set; }
        public string WallTypeName { get; set; }
        public string Name { get; set; }
        public string Story { get; set; }
        public string Section { get; set; }
        public bool IsLoadBearing { get; set; }
        public double Orientation { get; set; } // Orientation angle in degrees
    }
}

