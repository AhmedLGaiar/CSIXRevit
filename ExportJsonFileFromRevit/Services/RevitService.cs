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
        private const double PositionTolerance = 0.01; // in feet (~3mm) - Used for spatial comparison tolerance
        private const double DimensionTolerance = 10.0; // in mm - Used for dimensional comparison tolerance

        // ProcessFrame: Combines beam and column data into a single FrameRCData object for unified processing
        public FrameRCData ProcessFrame(Document doc)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            FrameRCData frameData = new FrameRCData();
            ProcessBeams(doc, frameData.beamRCDatas);
            ProcessColumns(doc, frameData.columnRCDatas);
            return frameData;
        }

        // ProcessBeams: Creates beams in the Revit document based on JSON data, handling duplicates and conflicts
        public void ProcessBeams(Document doc, List<BeamRCData> beams)
        {
            if (beams == null || beams.Count == 0)
            {
                TaskDialog.Show("Warning", "No beam data found.");
                return;
            }

            var existingBeams = GetExistingBeams(doc); // Retrieve existing beams to check for duplicates

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

                        var duplicateResult = CheckBeamDuplicates(existingBeams, startPoint, endPoint, beam); // Check for duplicate or conflicting beams
                        if (duplicateResult.HasExactDuplicate)
                        {
                            skipped++;
                            continue;
                        }
                        else if (duplicateResult.HasLocationConflict)
                        {
                            var userChoice = ShowLocationConflictDialog(beam, duplicateResult.ConflictingElements); // Prompt user to resolve conflicts
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

                        FamilySymbol symbol = GetOrCreateBeamType(doc, beam); // Retrieve or create the beam type
                        if (symbol == null)
                        {
                            TaskDialog.Show("Error", $"Cannot find or create beam type '{beam.SectionName}'.");
                            skipped++;
                            continue;
                        }

                        if (!symbol.IsActive) symbol.Activate();

                        Line beamLine = Line.CreateBound(startPoint, endPoint);
                        Level baseLevel = GetClosestLevel(doc, startPoint.Z); // Find the closest level for placement

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
                ShowFinalSummary("Beams", created, skipped, replaced); // Display summary of processing results
            }
        }

        // ProcessColumns: Creates columns in the Revit document based on JSON data, handling duplicates and conflicts
        public void ProcessColumns(Document doc, List<ColumnRCData> columns)
        {
            if (columns == null || columns.Count == 0)
            {
                TaskDialog.Show("Warning", "No column data found.");
                return;
            }

            var existingColumns = GetExistingColumns(doc); // Retrieve existing columns to check for duplicates

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

                        var duplicateResult = CheckColumnDuplicates(existingColumns, basePoint, topPoint, column); // Check for duplicate or conflicting columns
                        if (duplicateResult.HasExactDuplicate)
                        {
                            skipped++;
                            continue;
                        }
                        else if (duplicateResult.HasLocationConflict)
                        {
                            var userChoice = ShowLocationConflictDialog(column, duplicateResult.ConflictingElements); // Prompt user to resolve conflicts
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

                        FamilySymbol symbol = GetOrCreateColumnType(doc, column); // Retrieve or create the column type
                        if (symbol == null)
                        {
                            TaskDialog.Show("Error", $"Cannot find or create column type '{column.SectionName}'.");
                            skipped++;
                            continue;
                        }

                        if (!symbol.IsActive) symbol.Activate();

                        Line columnLine = Line.CreateBound(basePoint, topPoint);
                        Level baseLevel = GetClosestLevel(doc, basePoint.Z); // Find the closest level for placement

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
                ShowFinalSummary("Columns", created, skipped, replaced); // Display summary of processing results
            }
        }

        // ProcessWalls: Creates walls in the Revit document based on JSON data, handling duplicates and conflicts
        public void ProcessWalls(Document doc, List<WallRCData> walls)
        {
            if (walls == null || walls.Count == 0)
            {
                TaskDialog.Show("Warning", "No wall data found.");
                return;
            }

            var existingWalls = GetExistingWalls(doc); // Retrieve existing walls to check for duplicates

            using (Transaction tx = new Transaction(doc, "Create Walls"))
            {
                tx.Start();
                int created = 0, skipped = 0, replaced = 0;

                foreach (var wall in walls)
                {
                    try
                    {
                        // Convert from meters (JSON) to feet (Revit internal)
                        XYZ startPoint = new XYZ(wall.StartPoint.X * 3.28084, wall.StartPoint.Y * 3.28084, wall.StartPoint.Z * 3.28084);
                        XYZ endPoint = new XYZ(wall.EndPoint.X * 3.28084, wall.EndPoint.Y * 3.28084, wall.EndPoint.Z * 3.28084);

                        // Convert thickness and height from meters to mm for consistency
                        double thicknessInMm = wall.Thickness * 1000;
                        double heightInMm = wall.Height * 1000;

                        var duplicateResult = CheckWallDuplicates(existingWalls, startPoint, endPoint, wall, thicknessInMm, heightInMm); // Check for duplicate or conflicting walls
                        if (duplicateResult.HasExactDuplicate)
                        {
                            skipped++;
                            continue;
                        }
                        else if (duplicateResult.HasLocationConflict)
                        {
                            var userChoice = ShowLocationConflictDialog(wall, duplicateResult.ConflictingElements); // Prompt user to resolve conflicts
                            if (userChoice == DuplicateAction.Skip)
                            {
                                skipped++;
                                continue;
                            }
                            else if (userChoice == DuplicateAction.Replace)
                            {
                                foreach (var existing in duplicateResult.ConflictingElements)
                                {
                                    doc.Delete(existing.WallInstance.Id);
                                    existingWalls.Remove(existing);
                                }
                                replaced++;
                            }
                        }

                        WallType wallType = GetOrCreateWallType(doc, wall); // Retrieve or create the wall type
                        if (wallType == null)
                        {
                            TaskDialog.Show("Error", $"Cannot find or create wall type '{wall.WallTypeName}'.");
                            skipped++;
                            continue;
                        }

                        Line wallLine = Line.CreateBound(startPoint, endPoint);
                        Level baseLevel = GetClosestLevel(doc, startPoint.Z); // Find the closest level for placement
                        double levelOffset = startPoint.Z - baseLevel.Elevation;

                        Wall wallInstance = Wall.Create(doc, wallLine, wallType.Id, baseLevel.Id, heightInMm / 304.8, levelOffset, false, true);

                        if (wallInstance != null)
                        {
                            if (!string.IsNullOrEmpty(wall.Name))
                            {
                                Parameter nameParam = wallInstance.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                                if (nameParam != null && !nameParam.IsReadOnly)
                                {
                                    nameParam.Set(wall.Name);
                                }
                            }

                            existingWalls.Add(new ExistingWallInfo
                            {
                                WallInstance = wallInstance,
                                StartPoint = startPoint,
                                EndPoint = endPoint,
                                Thickness = thicknessInMm,
                                Height = heightInMm,
                                TypeName = wall.WallTypeName,
                                WallName = wall.Name
                            });

                            created++;
                        }
                        else
                        {
                            TaskDialog.Show("Error", $"Failed to create wall '{wall.Name}'.");
                            skipped++;
                        }
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Error", $"Failed to create wall '{wall.Name}': {ex.Message}");
                        skipped++;
                    }
                }

                tx.Commit();
                ShowFinalSummary("Walls", created, skipped, replaced); // Display summary of processing results
            }
        }

        public void ProcessBeamsGeometry(Document doc, List<BeamGeometryData> beams)
        {
            // Placeholder: Process beam geometry data if additional geometry processing is required
        }

        public void ProcessColumnsGeometry(Document doc, List<ColumnGeometryData> columns)
        {
            // Placeholder: Process column geometry data if additional geometry processing is required
        }

        // GetExistingBeams: Retrieves all existing beams from the document to check for duplicates
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

        // GetExistingColumns: Retrieves all existing columns from the document to check for duplicates
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

        // GetExistingWalls: Retrieves all existing walls from the document to check for duplicates
        private List<ExistingWallInfo> GetExistingWalls(Document doc)
        {
            var existingWalls = new List<ExistingWallInfo>();
            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(Wall));
            foreach (Wall wall in collector)
            {
                var locationCurve = wall.Location as LocationCurve;
                if (locationCurve != null && locationCurve.Curve is Line line)
                {
                    var wallInfo = new ExistingWallInfo
                    {
                        WallInstance = wall,
                        StartPoint = line.GetEndPoint(0),
                        EndPoint = line.GetEndPoint(1),
                        TypeName = wall.WallType.Name
                    };
                    try
                    {
                        wallInfo.Thickness = wall.Width * 304.8;
                        Parameter heightParam = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
                        if (heightParam != null) wallInfo.Height = heightParam.AsDouble() * 304.8;
                        Parameter nameParam = wall.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                        if (nameParam != null) wallInfo.WallName = nameParam.AsString();
                    }
                    catch { }
                    existingWalls.Add(wallInfo);
                }
            }
            return existingWalls;
        }

        // CheckBeamDuplicates: Checks if a new beam conflicts with existing beams based on location and dimensions
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

        // CheckColumnDuplicates: Checks if a new column conflicts with existing columns based on location and dimensions
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

        // CheckWallDuplicates: Checks if a new wall conflicts with existing walls based on location and dimensions
        private DuplicateCheckResult<ExistingWallInfo> CheckWallDuplicates(List<ExistingWallInfo> existingWalls, XYZ newStart, XYZ newEnd, WallRCData newWall, double thicknessInMm, double heightInMm)
        {
            var result = new DuplicateCheckResult<ExistingWallInfo>();
            foreach (var existing in existingWalls)
            {
                bool sameLocation =
                    (ArePointsClose(newStart, existing.StartPoint, PositionTolerance) &&
                     ArePointsClose(newEnd, existing.EndPoint, PositionTolerance)) ||
                    (ArePointsClose(newStart, existing.EndPoint, PositionTolerance) &&
                     ArePointsClose(newEnd, existing.StartPoint, PositionTolerance));

                if (sameLocation)
                {
                    bool dimensionsMatch =
                        Math.Abs(thicknessInMm - existing.Thickness) < DimensionTolerance &&
                        Math.Abs(heightInMm - existing.Height) < DimensionTolerance;

                    if (dimensionsMatch)
                    {
                        result.HasExactDuplicate = true;
                        return result;
                    }
                    result.HasLocationConflict = true;
                    result.ConflictingElements.Add(existing);
                }
            }
            return result;
        }

        // ArePointsClose: Compares two points to determine if they are within a tolerance distance
        private bool ArePointsClose(XYZ point1, XYZ point2, double tolerance)
        {
            return Math.Abs(point1.X - point2.X) < tolerance &&
                   Math.Abs(point1.Y - point2.Y) < tolerance &&
                   Math.Abs(point1.Z - point2.Z) < tolerance;
        }

        // ShowLocationConflictDialog: Displays a dialog to resolve conflicts between new and existing elements
        private DuplicateAction ShowLocationConflictDialog<T1, T2>(T1 newItem, List<T2> conflictingItems)
        {
            string conflictInfo = newItem is BeamRCData beam
                ? $"Beam '{beam.SectionName}' ({beam.Width:F0}×{beam.Depth:F0}mm) at same location as:\n" +
                  string.Join("\n", conflictingItems.Select(i => i is ExistingBeamInfo bi ? $"• Existing beam '{bi.TypeName}' ({bi.Width:F0}×{bi.Depth:F0}mm)" : ""))
                : newItem is ColumnRCData col
                    ? $"Column '{col.SectionName}' ({col.Width:F0}×{col.Depth:F0}mm) at same location as:\n" +
                      string.Join("\n", conflictingItems.Select(i => i is ExistingColumnInfo ci ? $"• Existing column '{ci.TypeName}' ({ci.Width:F0}×{ci.Depth:F0}mm)" : ""))
                    : newItem is WallRCData wall
                        ? $"Wall '{wall.Name}' ({(wall.Thickness * 1000):F0}mm thick, {(wall.Height * 1000):F0}mm high) at same location as:\n" +
                          string.Join("\n", conflictingItems.Select(i => i is ExistingWallInfo wi ? $"• Existing wall '{wi.WallName ?? wi.TypeName}' ({wi.Thickness:F0}mm thick, {wi.Height:F0}mm high)" : ""))
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

        // ShowFinalSummary: Displays a summary of the number of elements created, skipped, and replaced
        private void ShowFinalSummary(string elementType, int created, int skipped, int replaced)
        {
            string summary = $"{elementType} processing completed:\n\n" +
                            $"Created: {created}\n" +
                            $"Skipped: {skipped}\n" +
                            $"Replaced: {replaced}\n\n" +
                            $"Total: {created + skipped + replaced}";
            TaskDialog.Show("Completed", summary);
        }

        // GetOrCreateBeamType: Retrieves an existing beam type or uses a fallback if the specified type is not found
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

        // GetOrCreateColumnType: Retrieves an existing column type or uses a fallback if the specified type is not found
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

        // GetOrCreateWallType: Retrieves an existing wall type or uses a fallback if the specified type is not found
        private WallType GetOrCreateWallType(Document doc, WallRCData wall)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(WallType));
            foreach (WallType wallType in collector)
            {
                if (wallType.Name.Equals(wall.WallTypeName, StringComparison.OrdinalIgnoreCase) ||
                    wallType.Name.Equals(wall.Section, StringComparison.OrdinalIgnoreCase))
                    return wallType;
            }
            WallType fallbackWallType = collector.Cast<WallType>().FirstOrDefault(wt => wt.Kind == WallKind.Basic);
            if (fallbackWallType != null)
            {
                TaskDialog.Show("Warning", $"Wall type '{wall.WallTypeName}' not found. Using '{fallbackWallType.Name}' instead.");
                return fallbackWallType;
            }
            return null;
        }

        // GetClosestLevel: Finds the closest level in the document based on elevation for element placement
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