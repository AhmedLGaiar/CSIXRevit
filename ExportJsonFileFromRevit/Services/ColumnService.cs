//using Autodesk.Revit.DB;
//using Autodesk.Revit.DB.Structure;
//using Autodesk.Revit.UI;
//using ElementsData.Steel;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using ExportJsonFileFromRevit.Models;

//namespace ExportJsonFileFromRevit.Services
//{
//    public class ColumnService
//    {
//        private const double PositionTolerance = 0.01; // in feet (~3mm) - Used for spatial comparison tolerance
//        private const double DimensionTolerance = 10.0; // in mm - Used for dimensional comparison tolerance

//        // ProcessColumns: Creates columns in the Revit document based on JSON data, handling duplicates and conflicts
//        public void ProcessColumns(Document doc, List<ColumnRCData> columns)
//        {
//            if (columns == null || columns.Count == 0)
//            {
//                TaskDialog.Show("Warning", "No column data found.");
//                return;
//            }

//            var existingColumns = GetExistingColumns(doc); // Retrieve existing columns to check for duplicates

//            using (Transaction tx = new Transaction(doc, "Create Columns"))
//            {
//                tx.Start();

//                int created = 0, skipped = 0, replaced = 0;

//                foreach (var column in columns)
//                {
//                    try
//                    {
//                        XYZ basePoint = new XYZ(column.BasePoint.X / 304.8, column.BasePoint.Y / 304.8, column.BasePoint.Z / 304.8);
//                        XYZ topPoint = new XYZ(column.TopPoint.X / 304.8, column.TopPoint.Y / 304.8, column.TopPoint.Z / 304.8);

//                        var duplicateResult = CheckColumnDuplicates(existingColumns, basePoint, topPoint, column); // Check for duplicate or conflicting columns
//                        if (duplicateResult.HasExactDuplicate)
//                        {
//                            skipped++;
//                            continue;
//                        }
//                        else if (duplicateResult.HasLocationConflict)
//                        {
//                            var userChoice = ShowLocationConflictDialog(column, duplicateResult.ConflictingElements); // Prompt user to resolve conflicts
//                            if (userChoice == DuplicateAction.Skip)
//                            {
//                                skipped++;
//                                continue;
//                            }
//                            else if (userChoice == DuplicateAction.Replace)
//                            {
//                                foreach (var existing in duplicateResult.ConflictingElements)
//                                {
//                                    doc.Delete(existing.ColumnInstance.Id);
//                                    existingColumns.Remove(existing);
//                                }
//                                replaced++;
//                            }
//                        }

//                        FamilySymbol symbol = GetOrCreateColumnType(doc, column); // Retrieve or create the column type
//                        if (symbol == null)
//                        {
//                            TaskDialog.Show("Error", $"Cannot find or create column type '{column.SectionName}'.");
//                            skipped++;
//                            continue;
//                        }

//                        if (!symbol.IsActive) symbol.Activate();

//                        Line columnLine = Line.CreateBound(basePoint, topPoint);
//                        Level baseLevel = GetClosestLevel(doc, basePoint.Z); // Find the closest level for placement

//                        if (baseLevel == null)
//                        {
//                            TaskDialog.Show("Error", "No levels found in document.");
//                            break;
//                        }

//                        FamilyInstance columnInstance = doc.Create.NewFamilyInstance(
//                            columnLine, symbol, baseLevel, StructuralType.Column);

//                        existingColumns.Add(new ExistingColumnInfo
//                        {
//                            ColumnInstance = columnInstance,
//                            BasePoint = basePoint,
//                            TopPoint = topPoint,
//                            Width = column.Width,
//                            Depth = column.Depth,
//                            TypeName = column.SectionName
//                        });

//                        created++;
//                    }
//                    catch (Exception ex)
//                    {
//                        TaskDialog.Show("Error", $"Failed to create column '{column.SectionName}': {ex.Message}");
//                        skipped++;
//                    }
//                }

//                tx.Commit();
//                ShowFinalSummary("Columns", created, skipped, replaced); // Display summary of processing results
//            }
//        }

//        // GetExistingColumns: Retrieves all existing columns from the document to check for duplicates
//        private List<ExistingColumnInfo> GetExistingColumns(Document doc)
//        {
//            var existingColumns = new List<ExistingColumnInfo>();

//            var collector = new FilteredElementCollector(doc)
//                .OfClass(typeof(FamilyInstance))
//                .OfCategory(BuiltInCategory.OST_StructuralColumns);

//            foreach (FamilyInstance column in collector)
//            {
//                if (column.StructuralType == StructuralType.Column)
//                {
//                    var locationCurve = column.Location as LocationCurve;
//                    if (locationCurve != null && locationCurve.Curve is Line line)
//                    {
//                        var columnInfo = new ExistingColumnInfo
//                        {
//                            ColumnInstance = column,
//                            BasePoint = line.GetEndPoint(0),
//                            TopPoint = line.GetEndPoint(1),
//                            TypeName = column.Symbol.Name
//                        };

//                        try
//                        {
//                            var widthParam = column.Symbol.LookupParameter("b") ??
//                                            column.Symbol.LookupParameter("Width") ??
//                                            column.Symbol.LookupParameter("Column Width") ??
//                                            column.Symbol.LookupParameter("w");

//                            var depthParam = column.Symbol.LookupParameter("h") ??
//                                            column.Symbol.LookupParameter("Depth") ??
//                                            column.Symbol.LookupParameter("Column Depth") ??
//                                            column.Symbol.LookupParameter("d");

//                            if (widthParam != null && depthParam != null)
//                            {
//                                columnInfo.Width = widthParam.AsDouble() * 304.8; // Convert to mm
//                                columnInfo.Depth = depthParam.AsDouble() * 304.8; // Convert to mm
//                            }
//                        }
//                        catch
//                        {
//                            // Ignore parameter errors
//                        }

//                        existingColumns.Add(columnInfo);
//                    }
//                }
//            }
//            return existingColumns;
//        }

//        // CheckColumnDuplicates: Checks if a new column conflicts with existing columns based on location and dimensions
//        private DuplicateCheckResult<ExistingColumnInfo> CheckColumnDuplicates(List<ExistingColumnInfo> existingColumns, XYZ basePoint, XYZ topPoint, ColumnRCData column)
//        {
//            var duplicateResult = new DuplicateCheckResult<ExistingColumnInfo>();

//            foreach (var existing in existingColumns)
//            {
//                if (existing.BasePoint != null && existing.TopPoint != null)
//                {
//                    bool sameBaseLocation =
//                        Math.Abs(basePoint.X - existing.BasePoint.X) < PositionTolerance &&
//                        Math.Abs(basePoint.Y - existing.BasePoint.Y) < PositionTolerance &&
//                        Math.Abs(basePoint.Z - existing.BasePoint.Z) < DimensionTolerance;

//                    bool sameTopLocation =
//                        Math.Abs(topPoint.X - existing.TopPoint.X) < PositionTolerance &&
//                        Math.Abs(topPoint.Y - existing.TopPoint.Y) < PositionTolerance &&
//                        Math.Abs(topPoint.Z - existing.TopPoint.Z) < DimensionTolerance;

//                    if (sameBaseLocation && sameTopLocation)
//                    {
//                        bool dimensionsMatch =
//                            Math.Abs(column.Width - existing.Width) < DimensionTolerance &&
//                            Math.Abs(column.Depth - existing.Depth) < DimensionTolerance;

//                        if (dimensionsMatch)
//                        {
//                            duplicateResult.HasExactDuplicate = true;
//                            return duplicateResult;
//                        }
//                        duplicateResult.HasLocationConflict = true;
//                        duplicateResult.ConflictingElements.Add(existing);
//                    }
//                }
//            }
//            return duplicateResult;
//        }

//        // GetOrCreateColumnType: Retrieves an existing column type or uses a fallback if the specified type is not found
//        private FamilySymbol GetOrCreateColumnType(Document doc, ColumnRCData column)
//        {
//            var collector = new FilteredElementCollector(doc)
//                .OfClass(typeof(FamilySymbol))
//                .OfCategory(BuiltInCategory.OST_StructuralColumns);

//            foreach (FamilySymbol symbol in collector)
//            {
//                if (symbol.Name.Equals(column.SectionName, StringComparison.OrdinalIgnoreCase))
//                    return symbol;
//            }

//            var fallbackSymbol = collector.Cast<FamilySymbol>().FirstOrDefault();
//            if (fallbackSymbol != null)
//            {
//                TaskDialog.Show("Warning", $"Column type '{column.SectionName}' not found. Using '{fallbackSymbol.Name}' instead.");
//                return fallbackSymbol;
//            }

//            return null;
//        }

//        // ShowLocationConflictDialog: Displays a dialog to resolve conflicts between new and existing columns
//        private DuplicateAction ShowLocationConflictDialog(ColumnRCData newItem, List<ExistingColumnInfo> conflictingItems)
//        {
//            string conflictInfo = $"Column '{newItem.SectionName}' ({newItem.Width:F0}×{newItem.Depth:F0}mm) at same location as:\n" +
//                                 string.Join("\n", conflictingItems.Select(i => $"• Existing column '{i.TypeName}' ({i.Width:F0}×{i.Depth:F0}mm)"));

//            conflictInfo += "\nWhat would you like to do?";

//            var dialog = new TaskDialog("Column Location Conflict")
//            {
//                MainInstruction = "Existing column at same location",
//                MainContent = conflictInfo,
//                CommonButtons = TaskDialogCommonButtons.None
//            };

//            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Create both", "Keep existing column and create new one");
//            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Replace", "Delete existing column and create new one");
//            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "Skip", "Keep existing column and skip new one");

//            dialog.DefaultButton = TaskDialogResult.CommandLink3;

//            TaskDialogResult dialogResult = dialog.Show();

//            return dialogResult switch
//            {
//                TaskDialogResult.CommandLink1 => DuplicateAction.CreateBoth,
//                TaskDialogResult.CommandLink2 => DuplicateAction.Replace,
//                _ => DuplicateAction.Skip
//            };
//        }

//        // ShowFinalSummary: Displays a summary of the number of columns created, skipped, and replaced
//        private void ShowFinalSummary(string elementType, int created, int skipped, int replaced)
//        {
//            string summary = $"{elementType} processing completed:\n\n" +
//                            $"Created: {created}\n" +
//                            $"Skipped: {skipped}\n" +
//                            $"Replaced: {replaced}\n\n" +
//                            $"Total: {created + skipped + replaced}";
//            TaskDialog.Show("Completed", summary);
//        }

//        // GetClosestLevel: Finds the closest level in the document based on elevation for column placement
//        private Level GetClosestLevel(Document doc, double elevation)
//        {
//            Level closest = null;
//            double minDiff = double.MaxValue;

//            var levels = new FilteredElementCollector(doc)
//                .OfClass(typeof(Level))
//                .Cast<Level>();

//            foreach (var level in levels)
//            {
//                double diff = Math.Abs(level.Elevation - elevation);
//                if (diff < minDiff)
//                {
//                    minDiff = diff;
//                    closest = level;
//                }
//            }

//            return closest;
//        }

//        // ArePointsClose: Compares two points to determine if they are within a tolerance distance
//        private bool ArePointsClose(XYZ point1, XYZ point2, double tolerance)
//        {
//            return Math.Abs(point1.X - point2.X) < tolerance &&
//                   Math.Abs(point1.Y - point2.Y) < tolerance &&
//                   Math.Abs(point1.Z - point2.Z) < tolerance;
//        }
//    }
//}