using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using ElementsData.Steel;
using ElementsData.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ExportJsonFileFromRevit.Models;

namespace ExportJsonFileFromRevit.Services
{
    public class RevitService : IRevitService
    {
        private const double PositionTolerance = 0.01; // in feet (~3mm)
        private const double DimensionTolerance = 10.0; // in mm

        public FrameRCData ProcessFrame(Document doc)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            FrameRCData frameData = new FrameRCData();
            ProcessBeams(doc, frameData.beamRCDatas);
            ProcessColumns(doc, frameData.columnRCDatas);
            return frameData;
        }

        public void ProcessBeams(Document doc, List<BeamRCData> beams)
        {
            if (beams == null || beams.Count == 0)
            {
                TaskDialog.Show("Warning", "No beam data found.");
                return;
            }

            var existingBeams = GetExistingBeams(doc);

            using (Transaction tx = new Transaction(doc, "Create Beams"))
            {
                tx.Start();

                int created = 0, skipped = 0, replaced = 0;

                foreach (var beam in beams)
                {
                    try
                    {
                        XYZ startPoint = new XYZ(beam.StartPoint.X / 304.8, beam.StartPoint.Y / 304.8, beam.StartPoint.Z / 304.8);
                        XYZ endPoint = new XYZ(beam.EndPoint.X / 304.8, beam.EndPoint.Y / 304.8, beam.EndPoint.Z / 304.8);

                        var duplicateResult = CheckBeamDuplicates(existingBeams, startPoint, endPoint, beam);
                        if (duplicateResult.HasExactDuplicate)
                        {
                            skipped++;
                            continue;
                        }
                        else if (duplicateResult.HasLocationConflict)
                        {
                            var userChoice = ShowLocationConflictDialog(beam, duplicateResult.ConflictingElements);
                            if (userChoice == DuplicateAction.Skip)
                            {
                                skipped++;
                                continue;
                            }
                            else if (userChoice == DuplicateAction.Replace)
                            {
                                foreach (var existing in duplicateResult.ConflictingElements)
                                {
                                    doc.Delete(existing.BeamInstance.Id);
                                    existingBeams.Remove(existing);
                                }
                                replaced++;
                            }
                        }

                        FamilySymbol symbol = GetOrCreateBeamType(doc, beam);
                        if (symbol == null)
                        {
                            TaskDialog.Show("Error", $"Cannot find or create beam type '{beam.SectionName}'.");
                            skipped++;
                            continue;
                        }

                        if (!symbol.IsActive)
                            symbol.Activate();

                        Line beamLine = Line.CreateBound(startPoint, endPoint);
                        Level baseLevel = GetClosestLevel(doc, startPoint.Z);

                        if (baseLevel == null)
                        {
                            TaskDialog.Show("Error", "No levels found in document.");
                            break;
                        }

                        FamilyInstance beamInstance = doc.Create.NewFamilyInstance(
                            beamLine, symbol, baseLevel, StructuralType.Beam);

                        existingBeams.Add(new ExistingBeamInfo
                        {
                            BeamInstance = beamInstance,
                            StartPoint = startPoint,
                            EndPoint = endPoint,
                            Width = beam.Width,
                            Depth = beam.Depth,
                            TypeName = beam.SectionName
                        });

                        created++;
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Error", $"Failed to create beam '{beam.SectionName}': {ex.Message}");
                        skipped++;
                    }
                }

                tx.Commit();
                ShowFinalSummary("Beams", created, skipped, replaced);
            }
        }

        public void ProcessColumns(Document doc, List<ColumnRCData> columns)
        {
            if (columns == null || columns.Count == 0)
            {
                TaskDialog.Show("Warning", "No column data found.");
                return;
            }

            var existingColumns = GetExistingColumns(doc);

            using (Transaction tx = new Transaction(doc, "Create Columns"))
            {
                tx.Start();

                int created = 0, skipped = 0, replaced = 0;

                foreach (var column in columns)
                {
                    try
                    {
                        XYZ basePoint = new XYZ(column.BasePoint.X / 304.8, column.BasePoint.Y / 304.8, column.BasePoint.Z / 304.8);
                        XYZ topPoint = new XYZ(column.TopPoint.X / 304.8, column.TopPoint.Y / 304.8, column.TopPoint.Z / 304.8);

                        var duplicateResult = CheckColumnDuplicates(existingColumns, basePoint, topPoint, column);
                        if (duplicateResult.HasExactDuplicate)
                        {
                            skipped++;
                            continue;
                        }
                        else if (duplicateResult.HasLocationConflict)
                        {
                            var userChoice = ShowLocationConflictDialog(column, duplicateResult.ConflictingElements);
                            if (userChoice == DuplicateAction.Skip)
                            {
                                skipped++;
                                continue;
                            }
                            else if (userChoice == DuplicateAction.Replace)
                            {
                                foreach (var existing in duplicateResult.ConflictingElements)
                                {
                                    doc.Delete(existing.ColumnInstance.Id);
                                    existingColumns.Remove(existing);
                                }
                                replaced++;
                            }
                        }

                        FamilySymbol symbol = GetOrCreateColumnType(doc, column);
                        if (symbol == null)
                        {
                            TaskDialog.Show("Error", $"Cannot find or create column type '{column.SectionName}'.");
                            skipped++;
                            continue;
                        }

                        if (!symbol.IsActive)
                            symbol.Activate();

                        Line columnLine = Line.CreateBound(basePoint, topPoint);
                        Level baseLevel = GetClosestLevel(doc, basePoint.Z);

                        if (baseLevel == null)
                        {
                            TaskDialog.Show("Error", "No levels found in document.");
                            break;
                        }

                        FamilyInstance columnInstance = doc.Create.NewFamilyInstance(
                            columnLine, symbol, baseLevel, StructuralType.Column);

                        existingColumns.Add(new ExistingColumnInfo
                        {
                            ColumnInstance = columnInstance,
                            BasePoint = basePoint,
                            TopPoint = topPoint,
                            Width = column.Width,
                            Depth = column.Depth,
                            TypeName = column.SectionName
                        });

                        created++;
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Error", $"Failed to create column '{column.SectionName}': {ex.Message}");
                        skipped++;
                    }
                }

                tx.Commit();
                ShowFinalSummary("Columns", created, skipped, replaced);
            }
        }

        public void ProcessBeamsGeometry(Document doc, List<BeamGeometryData> beams)
        {
            // Implementation for BeamGeometryData (similar to ProcessBeams but using BeamGeometryData)
            // Add if needed
        }

        public void ProcessColumnsGeometry(Document doc, List<ColumnGeometryData> columns)
        {
            // Implementation for ColumnGeometryData (similar to ProcessColumns but using ColumnGeometryData)
            // Add if needed
        }

        private List<ExistingBeamInfo> GetExistingBeams(Document doc)
        {
            var existingBeams = new List<ExistingBeamInfo>();

            var collector = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_StructuralFraming);

            foreach (FamilyInstance beam in collector)
            {
                if (beam.StructuralType == StructuralType.Beam)
                {
                    var locationCurve = beam.Location as LocationCurve;
                    if (locationCurve != null && locationCurve.Curve is Line line)
                    {
                        var beamInfo = new ExistingBeamInfo
                        {
                            BeamInstance = beam,
                            StartPoint = line.GetEndPoint(0),
                            EndPoint = line.GetEndPoint(1),
                            TypeName = beam.Symbol.Name
                        };

                        try
                        {
                            var widthParam = beam.Symbol.LookupParameter("b") ??
                                            beam.Symbol.LookupParameter("Width") ??
                                            beam.Symbol.LookupParameter("Beam Width") ??
                                            beam.Symbol.LookupParameter("w");

                            var depthParam = beam.Symbol.LookupParameter("h") ??
                                            beam.Symbol.LookupParameter("Depth") ??
                                            beam.Symbol.LookupParameter("Beam Depth") ??
                                            beam.Symbol.LookupParameter("d");

                            if (widthParam != null && depthParam != null)
                            {
                                beamInfo.Width = widthParam.AsDouble() * 304.8; // Convert to mm
                                beamInfo.Depth = depthParam.AsDouble() * 304.8; // Convert to mm
                            }
                        }
                        catch
                        {
                            // Ignore parameter errors
                        }

                        existingBeams.Add(beamInfo);
                    }
                }
            }
            return existingBeams;
        }

        private List<ExistingColumnInfo> GetExistingColumns(Document doc)
        {
            var existingColumns = new List<ExistingColumnInfo>();

            var collector = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_StructuralColumns);

            foreach (FamilyInstance column in collector)
            {
                if (column.StructuralType == StructuralType.Column)
                {
                    var locationCurve = column.Location as LocationCurve;
                    if (locationCurve != null && locationCurve.Curve is Line line)
                    {
                        var columnInfo = new ExistingColumnInfo
                        {
                            ColumnInstance = column,
                            BasePoint = line.GetEndPoint(0),
                            TopPoint = line.GetEndPoint(1),
                            TypeName = column.Symbol.Name
                        };

                        try
                        {
                            var widthParam = column.Symbol.LookupParameter("b") ??
                                            column.Symbol.LookupParameter("Width") ??
                                            column.Symbol.LookupParameter("Column Width") ??
                                            column.Symbol.LookupParameter("w");

                            var depthParam = column.Symbol.LookupParameter("h") ??
                                            column.Symbol.LookupParameter("Depth") ??
                                            column.Symbol.LookupParameter("Column Depth") ??
                                            column.Symbol.LookupParameter("d");

                            if (widthParam != null && depthParam != null)
                            {
                                columnInfo.Width = widthParam.AsDouble() * 304.8; // Convert to mm
                                columnInfo.Depth = depthParam.AsDouble() * 304.8; // Convert to mm
                            }
                        }
                        catch
                        {
                            // Ignore parameter errors
                        }

                        existingColumns.Add(columnInfo);
                    }
                }
            }
            return existingColumns;
        }

        private DuplicateCheckResult<ExistingBeamInfo> CheckBeamDuplicates(List<ExistingBeamInfo> existingBeams, XYZ startPoint, XYZ endPoint, BeamRCData beam)
        {
            var duplicateResult = new DuplicateCheckResult<ExistingBeamInfo>();

            foreach (var existing in existingBeams)
            {
                bool sameLocation =
                    (ArePointsClose(startPoint, existing.StartPoint, PositionTolerance) &&
                     ArePointsClose(endPoint, existing.EndPoint, PositionTolerance)) ||
                    (ArePointsClose(startPoint, existing.EndPoint, PositionTolerance) &&
                     ArePointsClose(endPoint, existing.StartPoint, PositionTolerance));

                if (sameLocation)
                {
                    bool dimensionsMatch =
                        Math.Abs(beam.Width - existing.Width) < DimensionTolerance &&
                        Math.Abs(beam.Depth - existing.Depth) < DimensionTolerance;

                    if (dimensionsMatch)
                    {
                        duplicateResult.HasExactDuplicate = true;
                        return duplicateResult;
                    }
                    duplicateResult.HasLocationConflict = true;
                    duplicateResult.ConflictingElements.Add(existing);
                }
            }
            return duplicateResult;
        }

        private DuplicateCheckResult<ExistingColumnInfo> CheckColumnDuplicates(List<ExistingColumnInfo> existingColumns, XYZ basePoint, XYZ topPoint, ColumnRCData column)
        {
            var duplicateResult = new DuplicateCheckResult<ExistingColumnInfo>();

            foreach (var existing in existingColumns)
            {
                if (existing.BasePoint != null && existing.TopPoint != null)
                {
                    bool sameBaseLocation =
                        Math.Abs(basePoint.X - existing.BasePoint.X) < PositionTolerance &&
                        Math.Abs(basePoint.Y - existing.BasePoint.Y) < PositionTolerance &&
                        Math.Abs(basePoint.Z - existing.BasePoint.Z) < DimensionTolerance;

                    bool sameTopLocation =
                        Math.Abs(topPoint.X - existing.TopPoint.X) < PositionTolerance &&
                        Math.Abs(topPoint.Y - existing.TopPoint.Y) < PositionTolerance &&
                        Math.Abs(topPoint.Z - existing.TopPoint.Z) < DimensionTolerance;

                    if (sameBaseLocation && sameTopLocation)
                    {
                        bool dimensionsMatch =
                            Math.Abs(column.Width - existing.Width) < DimensionTolerance &&
                            Math.Abs(column.Depth - existing.Depth) < DimensionTolerance;

                        if (dimensionsMatch)
                        {
                            duplicateResult.HasExactDuplicate = true;
                            return duplicateResult;
                        }
                        duplicateResult.HasLocationConflict = true;
                        duplicateResult.ConflictingElements.Add(existing);
                    }
                }
            }
            return duplicateResult;
        }

        private bool ArePointsClose(XYZ point1, XYZ point2, double tolerance)
        {
            return Math.Abs(point1.X - point2.X) < tolerance &&
                   Math.Abs(point1.Y - point2.Y) < tolerance &&
                   Math.Abs(point1.Z - point2.Z) < tolerance;
        }

        private DuplicateAction ShowLocationConflictDialog<T1, T2>(T1 newItem, List<T2> conflictingItems)
        {
            string conflictInfo = newItem is BeamRCData beam
                ? $"Beam '{beam.SectionName}' ({beam.Width:F0}×{beam.Depth:F0}mm) at same location as:\n" +
                  string.Join("\n", conflictingItems.Select(i => i is ExistingBeamInfo bi ? $"• Existing beam '{bi.TypeName}' ({bi.Width:F0}×{bi.Depth:F0}mm)" : ""))
                : newItem is ColumnRCData col
                    ? $"Column '{col.SectionName}' ({col.Width:F0}×{col.Depth:F0}mm) at same location as:\n" +
                      string.Join("\n", conflictingItems.Select(i => i is ExistingColumnInfo ci ? $"• Existing column '{ci.TypeName}' ({ci.Width:F0}×{ci.Depth:F0}mm)" : ""))
                    : "Unknown element conflict";

            conflictInfo += "\nWhat would you like to do?";

            var dialog = new TaskDialog("Element Location Conflict")
            {
                MainInstruction = "Existing element at same location",
                MainContent = conflictInfo,
                CommonButtons = TaskDialogCommonButtons.None
            };

            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Create both", "Keep existing element and create new one");
            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Replace", "Delete existing element and create new one");
            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "Skip", "Keep existing element and skip new one");

            dialog.DefaultButton = TaskDialogResult.CommandLink3;

            TaskDialogResult dialogResult = dialog.Show();

            return dialogResult switch
            {
                TaskDialogResult.CommandLink1 => DuplicateAction.CreateBoth,
                TaskDialogResult.CommandLink2 => DuplicateAction.Replace,
                _ => DuplicateAction.Skip
            };
        }

        private void ShowFinalSummary(string elementType, int created, int skipped, int replaced)
        {
            string summary = $"{elementType} processing completed:\n\n" +
                            $"Created: {created}\n" +
                            $"Skipped: {skipped}\n" +
                            $"Replaced: {replaced}\n\n" +
                            $"Total: {created + skipped + replaced}";
            TaskDialog.Show("Completed", summary);
        }

        private FamilySymbol GetOrCreateBeamType(Document doc, BeamRCData beam)
        {
            var collector = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_StructuralFraming);

            foreach (FamilySymbol symbol in collector)
            {
                if (symbol.Name.Equals(beam.SectionName, StringComparison.OrdinalIgnoreCase))
                    return symbol;
            }

            var fallbackSymbol = collector.Cast<FamilySymbol>().FirstOrDefault();
            if (fallbackSymbol != null)
            {
                TaskDialog.Show("Warning", $"Beam type '{beam.SectionName}' not found. Using '{fallbackSymbol.Name}' instead.");
                return fallbackSymbol;
            }

            return null;
        }

        private FamilySymbol GetOrCreateColumnType(Document doc, ColumnRCData column)
        {
            var collector = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_StructuralColumns);

            foreach (FamilySymbol symbol in collector)
            {
                if (symbol.Name.Equals(column.SectionName, StringComparison.OrdinalIgnoreCase))
                    return symbol;
            }

            var fallbackSymbol = collector.Cast<FamilySymbol>().FirstOrDefault();
            if (fallbackSymbol != null)
            {
                TaskDialog.Show("Warning", $"Column type '{column.SectionName}' not found. Using '{fallbackSymbol.Name}' instead.");
                return fallbackSymbol;
            }

            return null;
        }

        private Level GetClosestLevel(Document doc, double elevation)
        {
            Level closest = null;
            double minDiff = double.MaxValue;

            var levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>();

            foreach (var level in levels)
            {
                double diff = Math.Abs(level.Elevation - elevation);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    closest = level;
                }
            }

            return closest;
        }
    }


}