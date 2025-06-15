//using Autodesk.Revit.DB;
//using Autodesk.Revit.DB.Structure;
//using Autodesk.Revit.UI;
//using ElementsData.Geometry;
//using ElementsData.Steel;
//using ExportJsonFileFromRevit.Models;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace ExportJsonFileFromRevit.Services
//{
//    public class WallService
//    {
//        private const double PositionTolerance = 0.01; // in feet (~3mm) - Used for spatial comparison tolerance
//        private const double DimensionTolerance = 10.0; // in mm - Used for dimensional comparison tolerance

//        // ProcessWalls: Creates walls in the Revit document based on JSON data, handling duplicates and conflicts
//        public void ProcessWalls(Document doc, List<WallRCData> walls)
//        {
//            if (walls == null || walls.Count == 0)
//            {
//                TaskDialog.Show("Warning", "No wall data found.");
//                return;
//            }

//            var existingWalls = GetExistingWalls(doc); // Retrieve existing walls to check for duplicates

//            using (Transaction tx = new Transaction(doc, "Create Walls"))
//            {
//                tx.Start();
//                int created = 0, skipped = 0, replaced = 0;

//                foreach (var wall in walls)
//                {
//                    try
//                    {
//                        // Convert from meters (JSON) to feet (Revit internal)
//                        XYZ startPoint = new XYZ(wall.StartPoint.X * 3.28084, wall.StartPoint.Y * 3.28084, wall.StartPoint.Z * 3.28084);
//                        XYZ endPoint = new XYZ(wall.EndPoint.X * 3.28084, wall.EndPoint.Y * 3.28084, wall.EndPoint.Z * 3.28084);

//                        // Convert thickness and height from meters to mm for consistency
//                        double thicknessInMm = wall.Thickness * 1000;
//                        double heightInMm = wall.Height * 1000;

//                        var duplicateResult = CheckWallDuplicates(existingWalls, startPoint, endPoint, wall, thicknessInMm, heightInMm); // Check for duplicate or conflicting walls
//                        if (duplicateResult.HasExactDuplicate)
//                        {
//                            skipped++;
//                            continue;
//                        }
//                        else if (duplicateResult.HasLocationConflict)
//                        {
//                            var userChoice = ShowLocationConflictDialog(wall, duplicateResult.ConflictingElements); // Prompt user to resolve conflicts
//                            if (userChoice == DuplicateAction.Skip)
//                            {
//                                skipped++;
//                                continue;
//                            }
//                            else if (userChoice == DuplicateAction.Replace)
//                            {
//                                foreach (var existing in duplicateResult.ConflictingElements)
//                                {
//                                    doc.Delete(existing.WallInstance.Id);
//                                    existingWalls.Remove(existing);
//                                }
//                                replaced++;
//                            }
//                        }

//                        WallType wallType = GetOrCreateWallType(doc, wall); // Retrieve or create the wall type
//                        if (wallType == null)
//                        {
//                            TaskDialog.Show("Error", $"Cannot find or create wall type '{wall.WallTypeName}'.");
//                            skipped++;
//                            continue;
//                        }

//                        Line wallLine = Line.CreateBound(startPoint, endPoint);
//                        Level baseLevel = GetClosestLevel(doc, startPoint.Z); // Find the closest level for placement
//                        double levelOffset = startPoint.Z - baseLevel.Elevation;

//                        Wall wallInstance = Wall.Create(doc, wallLine, wallType.Id, baseLevel.Id, heightInMm / 304.8, levelOffset, false, true);

//                        if (wallInstance != null)
//                        {
//                            if (!string.IsNullOrEmpty(wall.Name))
//                            {
//                                Parameter nameParam = wallInstance.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
//                                if (nameParam != null && !nameParam.IsReadOnly)
//                                {
//                                    nameParam.Set(wall.Name);
//                                }
//                            }

//                            existingWalls.Add(new ExistingWallInfo
//                            {
//                                WallInstance = wallInstance,
//                                StartPoint = startPoint,
//                                EndPoint = endPoint,
//                                Thickness = thicknessInMm,
//                                Height = heightInMm,
//                                TypeName = wall.WallTypeName,
//                                WallName = wall.Name
//                            });

//                            created++;
//                        }
//                        else
//                        {
//                            TaskDialog.Show("Error", $"Failed to create wall '{wall.Name}'.");
//                            skipped++;
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        TaskDialog.Show("Error", $"Failed to create wall '{wall.Name}': {ex.Message}");
//                        skipped++;
//                    }
//                }

//                tx.Commit();
//                ShowFinalSummary("Walls", created, skipped, replaced); // Display summary of processing results
//            }
//        }

//        // GetExistingWalls: Retrieves all existing walls from the document to check for duplicates
//        private List<ExistingWallInfo> GetExistingWalls(Document doc)
//        {
//            var existingWalls = new List<ExistingWallInfo>();
//            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(Wall));
//            foreach (Wall wall in collector)
//            {
//                var locationCurve = wall.Location as LocationCurve;
//                if (locationCurve != null && locationCurve.Curve is Line line)
//                {
//                    var wallInfo = new ExistingWallInfo
//                    {
//                        WallInstance = wall,
//                        StartPoint = line.GetEndPoint(0),
//                        EndPoint = line.GetEndPoint(1),
//                        TypeName = wall.WallType.Name
//                    };
//                    try
//                    {
//                        wallInfo.Thickness = wall.Width * 304.8;
//                        Parameter heightParam = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
//                        if (heightParam != null) wallInfo.Height = heightParam.AsDouble() * 304.8;
//                        Parameter nameParam = wall.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
//                        if (nameParam != null) wallInfo.WallName = nameParam.AsString();
//                    }
//                    catch { }
//                    existingWalls.Add(wallInfo);
//                }
//            }
//            return existingWalls;
//        }

//        // CheckWallDuplicates: Checks if a new wall conflicts with existing walls based on location and dimensions
//        private DuplicateCheckResult<ExistingWallInfo> CheckWallDuplicates(List<ExistingWallInfo> existingWalls, XYZ newStart, XYZ newEnd, WallRCData newWall, double thicknessInMm, double heightInMm)
//        {
//            var result = new DuplicateCheckResult<ExistingWallInfo>();
//            foreach (var existing in existingWalls)
//            {
//                bool sameLocation =
//                    (ArePointsClose(newStart, existing.StartPoint, PositionTolerance) &&
//                     ArePointsClose(newEnd, existing.EndPoint, PositionTolerance)) ||
//                    (ArePointsClose(newStart, existing.EndPoint, PositionTolerance) &&
//                     ArePointsClose(newEnd, existing.StartPoint, PositionTolerance));

//                if (sameLocation)
//                {
//                    bool dimensionsMatch =
//                        Math.Abs(thicknessInMm - existing.Thickness) < DimensionTolerance &&
//                        Math.Abs(heightInMm - existing.Height) < DimensionTolerance;

//                    if (dimensionsMatch)
//                    {
//                        result.HasExactDuplicate = true;
//                        return result;
//                    }
//                    result.HasLocationConflict = true;
//                    result.ConflictingElements.Add(existing);
//                }
//            }
//            return result;
//        }

//        // GetOrCreateWallType: Retrieves an existing wall type or uses a fallback if the specified type is not found
//        private WallType GetOrCreateWallType(Document doc, WallRCData wall)
//        {
//            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(WallType));
//            foreach (WallType wallType in collector)
//            {
//                if (wallType.Name.Equals(wall.WallTypeName, StringComparison.OrdinalIgnoreCase) ||
//                    wallType.Name.Equals(wall.Section, StringComparison.OrdinalIgnoreCase))
//                    return wallType;
//            }
//            WallType fallbackWallType = collector.Cast<WallType>().FirstOrDefault(wt => wt.Kind == WallKind.Basic);
//            if (fallbackWallType != null)
//            {
//                TaskDialog.Show("Warning", $"Wall type '{wall.WallTypeName}' not found. Using '{fallbackWallType.Name}' instead.");
//                return fallbackWallType;
//            }
//            return null;
//        }

//        // ShowLocationConflictDialog: Displays a dialog to resolve conflicts between new and existing walls
//        private DuplicateAction ShowLocationConflictDialog(WallRCData newItem, List<ExistingWallInfo> conflictingItems)
//        {
//            string conflictInfo = $"Wall '{newItem.Name}' ({(newItem.Thickness * 1000):F0}mm thick, {(newItem.Height * 1000):F0}mm high) at same location as:\n" +
//                                 string.Join("\n", conflictingItems.Select(i => $"• Existing wall '{i.WallName ?? i.TypeName}' ({i.Thickness:F0}mm thick, {i.Height:F0}mm high)"));

//            conflictInfo += "\nWhat would you like to do?";

//            var dialog = new TaskDialog("Wall Location Conflict")
//            {
//                MainInstruction = "Existing wall at same location",
//                MainContent = conflictInfo,
//                CommonButtons = TaskDialogCommonButtons.None
//            };

//            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Create both", "Keep existing wall and create new one");
//            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Replace", "Delete existing wall and create new one");
//            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "Skip", "Keep existing wall and skip new one");

//            dialog.DefaultButton = TaskDialogResult.CommandLink3;

//            TaskDialogResult dialogResult = dialog.Show();

//            return dialogResult switch
//            {
//                TaskDialogResult.CommandLink1 => DuplicateAction.CreateBoth,
//                TaskDialogResult.CommandLink2 => DuplicateAction.Replace,
//                _ => DuplicateAction.Skip
//            };
//        }

//        // ShowFinalSummary: Displays a summary of the number of walls created, skipped, and replaced
//        private void ShowFinalSummary(string elementType, int created, int skipped, int replaced)
//        {
//            string summary = $"{elementType} processing completed:\n\n" +
//                            $"Created: {created}\n" +
//                            $"Skipped: {skipped}\n" +
//                            $"Replaced: {replaced}\n\n" +
//                            $"Total: {created + skipped + replaced}";
//            TaskDialog.Show("Completed", summary);
//        }

//        // GetClosestLevel: Finds the closest level in the document based on elevation for wall placement
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