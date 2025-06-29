﻿using ElementsData.Geometry;

namespace ElementsData.Steel
{
    public class ColumnRCData
    {
        public double Width { get; set; }
        public double Depth { get; set; }
        public string SectionName { get; set; }
        public bool IsRectangle { get; set; }
        public bool ToBeDesign { get; set; }
        public double Cover { get; set; }
        public int RebarSize { get; set; }
        public int NumberR3Bars { get; set; }
        public int NumberR2Bars { get; set; }
        public int TieSize { get; set; }
        public double TieSpacingLongit { get; set; }
        public int Number2DirTieBars { get; set; }
        public int Number3DirTieBars { get; set; }
        public int SectionCount { get; set; }
        public string SymbolName { get; set; }

        public PointData BasePoint { get; set; }
        public PointData TopPoint { get; set; }
    }
}
