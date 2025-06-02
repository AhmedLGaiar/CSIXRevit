using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using ElementsData.Geometry;
using GeometryToRevit.ExistingInfo;
using GeometryToRevit.Utilities;

namespace ExportJsonFileFromRevit
{
    [Transaction(TransactionMode.Manual)]
    public partial class DrawColumns : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var doc = commandData.Application.ActiveUIDocument.Document;
            string jsonPath = @"C:\Users\Mahmoud\Desktop\columns_export.json";

            if (!File.Exists(jsonPath))
            {
                TaskDialog.Show("Error", "JSON file not found.");
                return Result.Failed;
            }

            List<ColumnGeometryData> columns;
            try
            {
                columns = JsonConvert.DeserializeObject<List<ColumnGeometryData>>(File.ReadAllText(jsonPath));
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Failed to read JSON file: {ex.Message}");
                return Result.Failed;
            }

            if (columns == null || columns.Count == 0)
            {
                TaskDialog.Show("Warning", "No column data found in JSON file.");
                return Result.Succeeded;
            }
               List<ExistingColumnInfo> existingColumns = GetExistingColumns(doc);



            using (Transaction tx = new Transaction(doc, "Create Columns"))
            {
                tx.Start();

                int created = 0;
                int skipped = 0;
                int replaced = 0;


                foreach (var column in columns)
                {
                    try
                    {
                        // Convert from MM (JSON) to FEET (Revit internal)
                        XYZ basePoint = new XYZ(
                            column.BasePoint.X / 304.8,
                            column.BasePoint.Y / 304.8,
                            column.BasePoint.Z / 304.8
                        );

                        XYZ topPoint = new XYZ(
                            column.TopPoint.X / 304.8,
                            column.TopPoint.Y / 304.8,
                            column.TopPoint.Z / 304.8
                        );

                        // Check for duplicates
                        var duplicateResult = CheckForDuplicates(existingColumns, basePoint, column);

                        if (duplicateResult.HasExactDuplicate)
                        {
                            skipped++;
                            continue; // Skip creating column, keeping existing column unchanged
                        }
                        else if (duplicateResult.HasLocationConflict)
                        {
                            // Show user choice dialog for location conflicts (different dimensions)
                            var userChoice = ShowLocationConflictDialog(column, duplicateResult.ConflictingColumns);

                            if (userChoice == DuplicateAction.Skip)
                            {
                                skipped++;
                                continue;
                            }
                            else if (userChoice == DuplicateAction.Replace)
                            {
                                // Delete existing columns at this location
                                foreach (var existingColumn in duplicateResult.ConflictingColumns)
                                {
                                    doc.Delete(existingColumn.ColumnInstance.Id);
                                }
                                // Remove from our tracking list
                                foreach (var toRemove in duplicateResult.ConflictingColumns)
                                {
                                    existingColumns.Remove(toRemove);
                                }
                                replaced++;
                            }
                            // If userChoice == DuplicateAction.CreateBoth, continue with creation
                        }

                        // Create new column
                        FamilySymbol symbol = GetOrCreateColumnType(doc, column);
                        if (symbol == null)
                        {
                            TaskDialog.Show("Error", $"Cannot find or create column type '{column.SectionName}'. This column will be skipped.");
                            skipped++;
                            continue;
                        }

                        if (!symbol.IsActive)
                            symbol.Activate();

                        Level baseLevel = GetClosestLevel(doc, basePoint.Z);

                        if (baseLevel == null)
                        {
                            TaskDialog.Show("Error", "No levels found in document. Cannot create columns.");
                            break;
                        }

                        FamilyInstance columnInstance = doc.Create.NewFamilyInstance(
                            basePoint,
                            symbol,
                            baseLevel,
                            StructuralType.Column
                        );

                        // FIXED: Add the newly created column to existing columns list for tracking
                        existingColumns.Add(new ExistingColumnInfo
                        {
                            ColumnInstance = columnInstance,
                            BasePoint = basePoint,
                            Depth = UnitUtils.ConvertToInternalUnits(column.Depth, UnitTypeId.Millimeters),
                            Width = UnitUtils.ConvertToInternalUnits(column.Width, UnitTypeId.Millimeters),
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

                // Show final summary
                ShowFinalSummary(created, skipped, replaced);
            }

            return Result.Succeeded;
        }

















        private List<ExistingColumnInfo> GetExistingColumns(Document doc)
        {
            var existingColumns = new List<ExistingColumnInfo>();

            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_StructuralColumns);

            foreach (FamilyInstance column in collector)
            {
                if (column.StructuralType == StructuralType.Column)
                {
                    var locationPoint = column.Location as LocationPoint;

                    if (locationPoint != null)
                    {
                        var columnInfo = new ExistingColumnInfo
                        {
                            ColumnInstance = column,
                            BasePoint = locationPoint.Point,
                            TypeName = column.Symbol.Name
                        };

                        try
                        {
                            // Try to get dimensions from parameters
                            Parameter widthParam = column.Symbol.LookupParameter("b") ??
                                          column.Symbol.LookupParameter("Width") ??
                                          column.Symbol.LookupParameter("Column Width") ??
                                          column.Symbol.LookupParameter("w");

                            Parameter depthParam = column.Symbol.LookupParameter("h") ??
                                                  column.Symbol.LookupParameter("Depth") ??
                                                  column.Symbol.LookupParameter("Column Depth") ??
                                                  column.Symbol.LookupParameter("d");

                            if (widthParam != null && depthParam != null)
                            {
                                columnInfo.Width = widthParam.AsDouble();
                                columnInfo.Depth = depthParam.AsDouble();
                            }
                        }
                        catch
                        {
                            // Ignore parameter reading errors
                        }

                        existingColumns.Add(columnInfo);
                    }
                }
            }

            return existingColumns;
        }





        private DuplicateCheckResult CheckForDuplicates(List<ExistingColumnInfo> existingColumns, XYZ newBase, ColumnGeometryData newColumn)
        {
            var result = new DuplicateCheckResult();
            const double positionTolerance = 0.01;  // in feet (about 3mm)
            const double dimensionTolerance = 0.01; // in feet (about 3mm)

            // Convert new column dimensions to feet once for all comparisons
            double newColumnWidthFeet = newColumn.Width / 304.8;  // mm to feet
            double newColumnDepthFeet = newColumn.Depth / 304.8;  // mm to feet

            foreach (var existing in existingColumns)
            {
                // Check that points are not null before comparison
                if (existing.BasePoint == null || newBase == null)
                    continue;

                // Compare NEW base point with OLD base point
                bool sameBaseLocation =
                   Math.Abs(newBase.X - existing.BasePoint.X) < positionTolerance &&
                   Math.Abs(newBase.Y - existing.BasePoint.Y) < positionTolerance &&
                   Math.Abs(newBase.Z - existing.BasePoint.Z) < positionTolerance;

                if (sameBaseLocation)
                {
                    // Compare NEW dimensions with OLD dimensions (both in feet)
                    bool dimensionsMatch =
                        Math.Abs(newColumnWidthFeet - existing.Width) < dimensionTolerance &&
                        Math.Abs(newColumnDepthFeet - existing.Depth) < dimensionTolerance;

                    if (dimensionsMatch)
                    {
                        // Same location AND same dimensions = exact duplicate
                        result.HasExactDuplicate = true;
                        return result; // Early exit - exact duplicate found
                    }
                    else
                    {
                        // Same location BUT different dimensions = location conflict
                        result.HasLocationConflict = true;
                        result.ConflictingColumns.Add(existing);
                    }
                }
            }

            return result;
        }






















        private DuplicateAction ShowLocationConflictDialog(ColumnGeometryData newColumn, List<ExistingColumnInfo> conflictingColumns)
        {
            double newWidthMM = newColumn.Width * 1000;
            double newDepthMM = newColumn.Depth * 1000;

            string conflictInfo = $"New column '{newColumn.SectionName}' ({newWidthMM:F0}×{newDepthMM:F0}mm) at the same location as:\n\n";

            foreach (var existing in conflictingColumns)
            {
                double widthMM = existing.Width * 1000;
                double depthMM = existing.Depth * 1000;
                conflictInfo += $"• Existing column '{existing.TypeName}' ({widthMM:F0}×{depthMM:F0}mm)\n";
            }

            conflictInfo += "\nWhat would you like to do?";

            TaskDialog dialog = new TaskDialog("Column Location Conflict")
            {
                MainInstruction = "Column exists at the same location",
                MainContent = conflictInfo,
                CommonButtons = TaskDialogCommonButtons.None
            };

            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Create both columns", "Keep existing column and create new one at same location");
            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Replace existing column", "Delete existing column and create new one");
            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "Skip new column", "Keep existing column and skip the new one");

            dialog.DefaultButton = TaskDialogResult.CommandLink3;

            TaskDialogResult result = dialog.Show();

            switch (result)
            {
                case TaskDialogResult.CommandLink1:
                    return DuplicateAction.CreateBoth;
                case TaskDialogResult.CommandLink2:
                    return DuplicateAction.Replace;
                case TaskDialogResult.CommandLink3:
                default:
                    return DuplicateAction.Skip;
            }
        }










        private void ShowFinalSummary(int created, int skipped, int replaced)
        {
            string summary = $"Column processing completed:\n\n" +
                           $"Created: {created}\n" +
                           $"Skipped: {skipped}\n" +
                           $"Replaced: {replaced}\n\n" +
                           $"Total: {created + skipped + replaced}";

            TaskDialog.Show("Completed", summary);
        }



        private FamilySymbol GetOrCreateColumnType(Document doc, ColumnGeometryData column)
        {
            // Try to find existing FamilySymbol
            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_StructuralColumns);

            foreach (FamilySymbol symbol in collector)
            {
                if (symbol.Name.Equals(column.SectionName, StringComparison.OrdinalIgnoreCase))
                    return symbol;
            }

            // If not found, use any structural column symbol as fallback
            FamilySymbol fallbackSymbol = collector.Cast<FamilySymbol>().FirstOrDefault();

            if (fallbackSymbol != null)
            {
                TaskDialog.Show("Warning", $"Column type '{column.SectionName}' not found. Using '{fallbackSymbol.Name}' instead.");
                return fallbackSymbol;
            }

            return null;
        }

        private Level GetClosestLevel(Document doc, double elevationFeet)
        {
            Level closest = null;
            double minDiff = double.MaxValue;

            var levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>();

            foreach (var level in levels)
            {
                double diff = Math.Abs(level.Elevation - elevationFeet);
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