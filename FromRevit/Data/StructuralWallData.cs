using System;
using System.Collections.Generic;

namespace FromRevit.Data
{
    public class StructuralWallData
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
        public string Name { get; set; }
        public string Story { get; set; }
        public string Section { get; set; }
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double StartZ { get; set; }
        public double EndX { get; set; }
        public double EndY { get; set; }
        public double EndZ { get; set; }
        public bool IsLoadBearing { get; set; }
        public double Orientation { get; set; }

        // New property to capture additional structural wall properties
        public Dictionary<string, string> AdditionalProperties { get; set; }
    }
}