using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using StructLink_X.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StructLink_X.Services
{
    public class RebarGenerator
    {
        private readonly Document _doc;

        public RebarGenerator(Document doc)
        {
            _doc = doc;
        }

        public void GenerateColumnRebar(ColumnRCData column, Element hostColumn)
        {
            using (Transaction tx = new Transaction(_doc, "Generate Column Rebar"))
            {
                tx.Start();

                // Get RebarBarType based on MainBarDiameter
                RebarBarType barType = new FilteredElementCollector(_doc)
                    .OfClass(typeof(RebarBarType))
                    .Cast<RebarBarType>()
                    .FirstOrDefault(bt => bt.BarNominalDiameter * 304.8 >= column.MainBarDiameter); // Convert feet to mm
                if (barType == null)
                    throw new System.Exception($"No RebarBarType found for diameter {column.MainBarDiameter} mm. Please define a RebarBarType in Revit.");

                // Get RebarShape (default: M_00 for straight rebar)
                RebarShape shape = new FilteredElementCollector(_doc)
                    .OfClass(typeof(RebarShape))
                    .Cast<RebarShape>()
                    .FirstOrDefault(rs => rs.Name == "M_00"); // M_00 is the standard straight rebar shape
                if (shape == null)
                    throw new System.Exception("No straight RebarShape (M_00) found. Please define it in Revit.");

                // Create rebar curves
                List<Curve> curves = new List<Curve>();
                double height = column.Height; // in meters
                double width = column.Width / 1000; // Convert mm to m
                double depth = column.Depth / 1000; // Convert mm to m
                double cover = column.ConcreteCover / 1000; // Convert mm to m
                int totalBars = column.NumBarsDir3 + column.NumBarsDir2;

                // Distribute bars symmetrically around the column cross-section
                for (int i = 0; i < totalBars; i++)
                {
                    double x = (i % 2 == 0 ? -1 : 1) * (width / 2 - cover);
                    double y = (i < totalBars / 2 ? -1 : 1) * (depth / 2 - cover);
                    curves.Add(Line.CreateBound(new XYZ(x, y, 0), new XYZ(x, y, height)));
                }

                // Create rebar
                Rebar rebar = Rebar.CreateFromCurves(
                    _doc,
                    RebarStyle.Standard,
                    barType,
                    null, // Start hook
                    null, // End hook
                    hostColumn,
                    new XYZ(0, 0, 1), // Normal (Z-axis for columns)
                    curves,
                    RebarHookOrientation.Right,
                    RebarHookOrientation.Right,
                    true, // Use existing shape
                    true); // Create new shape if needed

                // Generate ties (stirrups)
                GenerateColumnTies(column, hostColumn, tx);

                tx.Commit();
            }
        }

        private void GenerateColumnTies(ColumnRCData column, Element hostColumn, Transaction tx)
        {
            // Get RebarBarType for ties
            RebarBarType tieBarType = new FilteredElementCollector(_doc)
                .OfClass(typeof(RebarBarType))
                .Cast<RebarBarType>()
                .FirstOrDefault(bt => bt.BarNominalDiameter * 304.8 >= column.TieBarDiameter);
            if (tieBarType == null)
                throw new System.Exception($"No RebarBarType found for tie diameter {column.TieBarDiameter} mm. Please define a RebarBarType in Revit.");

            // Get RebarShape for ties (e.g., M_10 for rectangular ties)
            RebarShape tieShape = new FilteredElementCollector(_doc)
                .OfClass(typeof(RebarShape))
                .Cast<RebarShape>()
                .FirstOrDefault(rs => rs.Name == "M_10"); // M_10 is a typical rectangular tie shape
            if (tieShape == null)
                throw new System.Exception("No rectangular RebarShape (M_10) found. Please define it in Revit.");

            // Calculate tie geometry
            double width = column.Width / 1000; // Convert mm to m
            double depth = column.Depth / 1000; // Convert mm to m
            double cover = column.ConcreteCover / 1000; // Convert mm to m
            double tieSpacing = column.TieSpacing / 1000; // Convert mm to m
            double height = column.Height; // in meters
            int tieCount = (int)Math.Ceiling(height / tieSpacing);

            // Create tie curves
            List<Curve> tieCurves = new List<Curve>();
            double tieWidth = width - 2 * cover;
            double tieDepth = depth - 2 * cover;
            for (int i = 0; i < tieCount; i++)
            {
                double z = (i + 0.5) * tieSpacing; // Place ties at mid-spacing intervals
                if (z >= height) break;

                // Define rectangular tie path
                tieCurves.Add(Line.CreateBound(new XYZ(-tieWidth / 2, -tieDepth / 2, z), new XYZ(tieWidth / 2, -tieDepth / 2, z)));
                tieCurves.Add(Line.CreateBound(new XYZ(tieWidth / 2, -tieDepth / 2, z), new XYZ(tieWidth / 2, tieDepth / 2, z)));
                tieCurves.Add(Line.CreateBound(new XYZ(tieWidth / 2, tieDepth / 2, z), new XYZ(-tieWidth / 2, tieDepth / 2, z)));
                tieCurves.Add(Line.CreateBound(new XYZ(-tieWidth / 2, tieDepth / 2, z), new XYZ(-tieWidth / 2, -tieDepth / 2, z)));
            }

            // Create ties
            Rebar tieRebar = Rebar.CreateFromCurves(
                _doc,
                RebarStyle.StirrupTie,
                tieBarType,
                null,
                null,
                hostColumn,
                new XYZ(0, 0, 1), // Normal (Z-axis)
                tieCurves,
                RebarHookOrientation.Right,
                RebarHookOrientation.Right,
                true,
                true);
        }

        public void GenerateBeamRebar(BeamRCData beam, Element hostBeam)
        {
            using (Transaction tx = new Transaction(_doc, "Generate Beam Rebar"))
            {
                tx.Start();

                // Get RebarBarType
                RebarBarType barType = new FilteredElementCollector(_doc)
                    .OfClass(typeof(RebarBarType))
                    .Cast<RebarBarType>()
                    .FirstOrDefault(bt => bt.BarNominalDiameter * 304.8 >= beam.MainBarDiameter);
                if (barType == null)
                    throw new System.Exception($"No RebarBarType found for diameter {beam.MainBarDiameter} mm. Please define a RebarBarType in Revit.");

                // Get RebarShape
                RebarShape shape = new FilteredElementCollector(_doc)
                    .OfClass(typeof(RebarShape))
                    .Cast<RebarShape>()
                    .FirstOrDefault(rs => rs.Name == "M_00");
                if (shape == null)
                    throw new System.Exception("No straight RebarShape (M_00) found. Please define it in Revit.");

                // Create rebar curves
                List<Curve> curves = new List<Curve>();
                double length = beam.Length; // in meters
                double width = beam.Width / 1000; // Convert mm to m
                double depth = beam.Depth / 1000; // Convert mm to m
                double cover = beam.ConcreteCover / 1000; // Convert mm to m
                int totalBars = beam.BottomBars + beam.TopBars;

                // Distribute bars along the beam
                for (int i = 0; i < totalBars; i++)
                {
                    double x = length / 2;
                    double y = (i % 2 == 0 ? -1 : 1) * (width / 2 - cover);
                    double z = (i < beam.BottomBars ? -1 : 1) * (depth / 2 - cover);
                    curves.Add(Line.CreateBound(new XYZ(-x, y, z), new XYZ(x, y, z)));
                }

                // Create rebar
                Rebar rebar = Rebar.CreateFromCurves(
                    _doc,
                    RebarStyle.Standard,
                    barType,
                    null,
                    null,
                    hostBeam,
                    new XYZ(1, 0, 0), // Normal (X-axis for beams)
                    curves,
                    RebarHookOrientation.Right,
                    RebarHookOrientation.Right,
                    true,
                    true);

                // Generate ties (stirrups)
                GenerateBeamTies(beam, hostBeam, tx);

                tx.Commit();
            }
        }

        private void GenerateBeamTies(BeamRCData beam, Element hostBeam, Transaction tx)
        {
            // Get RebarBarType for ties
            RebarBarType tieBarType = new FilteredElementCollector(_doc)
                .OfClass(typeof(RebarBarType))
                .Cast<RebarBarType>()
                .FirstOrDefault(bt => bt.BarNominalDiameter * 304.8 >= beam.TieBarDiameter);
            if (tieBarType == null)
                throw new System.Exception($"No RebarBarType found for tie diameter {beam.TieBarDiameter} mm. Please define a RebarBarType in Revit.");

            // Get RebarShape for ties
            RebarShape tieShape = new FilteredElementCollector(_doc)
                .OfClass(typeof(RebarShape))
                .Cast<RebarShape>()
                .FirstOrDefault(rs => rs.Name == "M_10");
            if (tieShape == null)
                throw new System.Exception("No rectangular RebarShape (M_10) found. Please define it in Revit.");

            // Calculate tie geometry
            double length = beam.Length; // in meters
            double width = beam.Width / 1000; // Convert mm to m
            double depth = beam.Depth / 1000; // Convert mm to m
            double cover = beam.ConcreteCover / 1000; // Convert mm to m
            double tieSpacing = beam.TieSpacing / 1000; // Convert mm to m
            int tieCount = (int)Math.Ceiling(length / tieSpacing);

            // Create tie curves
            List<Curve> tieCurves = new List<Curve>();
            double tieWidth = width - 2 * cover;
            double tieDepth = depth - 2 * cover;
            for (int i = 0; i < tieCount; i++)
            {
                double x = -length / 2 + (i + 0.5) * tieSpacing; // Place ties along beam length
                if (Math.Abs(x) > length / 2) continue;

                // Define rectangular tie path
                tieCurves.Add(Line.CreateBound(new XYZ(x, -tieWidth / 2, -tieDepth / 2), new XYZ(x, tieWidth / 2, -tieDepth / 2)));
                tieCurves.Add(Line.CreateBound(new XYZ(x, tieWidth / 2, -tieDepth / 2), new XYZ(x, tieWidth / 2, tieDepth / 2)));
                tieCurves.Add(Line.CreateBound(new XYZ(x, tieWidth / 2, tieDepth / 2), new XYZ(x, -tieWidth / 2, tieDepth / 2)));
                tieCurves.Add(Line.CreateBound(new XYZ(x, -tieWidth / 2, tieDepth / 2), new XYZ(x, -tieWidth / 2, -tieDepth / 2)));
            }

            // Create ties
            Rebar tieRebar = Rebar.CreateFromCurves(
                _doc,
                RebarStyle.StirrupTie,
                tieBarType,
                null,
                null,
                hostBeam,
                new XYZ(1, 0, 0), // Normal (X-axis)
                tieCurves,
                RebarHookOrientation.Right,
                RebarHookOrientation.Right,
                true,
                true);
        }
    }
}