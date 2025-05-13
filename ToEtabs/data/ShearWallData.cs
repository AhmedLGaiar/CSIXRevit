using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToEtabs.Data;

namespace ToEtabs.Models
{
    public class ShearWallData
    {
        public string Id { get; set; }
        public PointData StartPoint { get; set; } // Start point coordinates in meters
        public PointData EndPoint { get; set; }   // End point coordinates in meters
        public double Length { get; set; }        // Length of wall in meters
        public double Thickness { get; set; }     // Thickness of wall in meters
        public double Height { get; set; }        // Height of wall in meters
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
        public Dictionary<string, string> AdditionalProperties { get; set; }
    }
}