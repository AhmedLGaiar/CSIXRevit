using StructLink_X.Models;
using System;

namespace StructLink_X.Services
{
    public class RebarCalculator
    {
        public static double CalculateColumnConcreteVolume(ColumnRCData column)
        {
            return (column.Width / 1000) * (column.Depth / 1000) * column.Height;
        }

        public static double CalculateBeamConcreteVolume(BeamRCData beam)
        {
            return (beam.Width / 1000) * (beam.Depth / 1000) * beam.Length;
        }

        public static double CalculateColumnRebarLength(ColumnRCData column)
        {
            double hookLength = 0.1; // 100 mm
            double lapLength = 0.5;  // 500 mm
            double totalBarLength = column.Height + hookLength + lapLength;
            return totalBarLength * (column.NumBarsDir3 + column.NumBarsDir2);
        }

        public static double CalculateColumnTieLength(ColumnRCData column)
        {
            double cover = column.ConcreteCover / 1000; // Convert mm to m
            double hookLength = 0.1; // 100 mm
            double perimeter = ((column.Width / 1000) + (column.Depth / 1000)) * 2 - 4 * cover + hookLength;
            double tieCount = Math.Ceiling(column.Height / (column.TieSpacing / 1000));
            return perimeter * tieCount;
        }

        public static double CalculateBeamRebarLength(BeamRCData beam)
        {
            double hookLength = 0.1; // 100 mm
            double lapLength = 0.5;  // 500 mm
            double totalBarLength = beam.Length + hookLength + lapLength;
            return totalBarLength * (beam.BottomBars + beam.TopBars);
        }

        public static double CalculateBeamTieLength(BeamRCData beam)
        {
            double cover = beam.ConcreteCover / 1000; // Convert mm to m
            double hookLength = 0.1; // 100 mm
            double perimeter = ((beam.Width / 1000) + (beam.Depth / 1000)) * 2 - 4 * cover + hookLength;
            return perimeter; // Return length of one tie
        }

        public static double CalculateRebarWeight(double length, int rebarSize)
        {
            return 0.00617 * rebarSize * rebarSize * length; // Weight in kg, rebarSize in mm
        }
    }
}