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
//    public class BeamService

//    {
//        private const double PositionTolerance = 0.01; // in feet (~3mm) - Used for spatial comparison tolerance
//        private const double DimensionTolerance = 10.0; // in mm - Used for dimensional comparison tolerance

//        // ProcessBeams: Creates beams in the Revit document based on JSON data, handling duplicates and conflicts
//        public void ProcessBeams(Document doc, List<BeamRCData> beams)
//        {
//            if (beams == null || beams.Count == 0)
//            {
//                TaskDialog.Show("Warning", "No beam data found.");
//                return;
//            }

//            var existingBeams = GetExistingBeams(doc); // Retrieve existing beams to check for duplicates

//            using (Transaction tx = new Transaction(doc, "Create Beams"))
//            {
//                tx.Start();

//                int created = 0, skipped = 0, replaced = 0;

//                foreach (var beam in beams)
//                {
//                    try
//                    {
//                        XYZ startPoint = new XYZ(beam.StartPoint.X / 304.8, beam.StartPoint.Y / 304.8, beam.StartPoint.Z / 304.8);
//                        XYZ endPoint = new XYZ(beam.EndPoint.X / 304.8, beam.EndPoint.Y / 304.8, beam.EndPoint.Z / 304.8);

//                        var duplicateResult = CheckBeamDuplicates(existingBeams, startPoint, endPoint, beam); // Check for duplicate or conflicting beams
//                        if (duplicateResult.HasExactDuplicate)
//                        {
//                            skipped++;
//                            continue;
//                        }
//                        else if (duplicateResult.HasLocationConflict)
//                        {
//                            var userChoice = ShowLocationConflictDialog(beam, duplicateResult.ConflictingElements); // Prompt user to resolve conflicts
//                            if (userChoice == DuplicateAction.Skip)
//                            {
//                                skipped++;
//                                continue;
//                            }
//                            else if (userChoice == DuplicateAction.Replace)
//                            {
//                                foreach (var existing in duplicateResult.ConflictingElements)
//                                {
//                                    doc.Delete(existing.BeamInstance.Id);
//                                    existingBeams.Remove(existing);
//                                }
//                                replaced++;
//                            }
//                        }

//                        FamilySymbol symbol = GetOrCreateBeamType(doc, beam); // Retrieve or create the beam type
//                        if (symbol == null)
//                        {
//                            TaskDialog.Show("Error", $"Cannot find or create beam type '{beam.SectionName}'.");
//                            skipped++;
//                            continue;
//                        }

//                        if (!symbol.IsActive) symbol.Activate();

//                        Line beamLine = Line.CreateBound(startPoint, endPoint);
//                        Level baseLevel = GetClosestLevel(doc, startPoint.Z); // Find the closest level for placement

//                        if (baseLevel == null)
//                        {
//                            TaskDialog.Show("Error", "No levels found in document.");
//                            break;
//                        }

//                        FamilyInstance beamInstance = doc.Create.NewFamilyInstance(
//                            beamLine, symbol, baseLevel, StructuralType.Beam);

//                        existingBeams.Add(new ExistingBeamInfo
//                        {
//                            BeamInstance = beamInstance,
//                            StartPoint = startPoint,
//                            EndPoint = endPoint,
//                            Width = beam.Width,
//                            Depth = beam.Depth,
//                            TypeName = beam.SectionName
//                        });

//                        created++;
//                    }
//                    catch (Exception ex)
//                    {
//                        TaskDialog.Show("Error", $"Failed to create beam '{beam.SectionName}': {ex.Message}");
//                        skipped++;
//                    }
//                }

//                tx.Commit();
//                ShowFinalSummary("Beams", created, skipped, replaced); // Display summary of processing results
//            }
//        }

//        // GetExistingBeams: Retrieves all existing beams from the document to check for duplicates
//        private List<ExistingBeamInfo> GetExistingBeams(Document doc)
//        {
//            var existingBeams = new List<ExistingBeamInfo>();

//            var collector = new FilteredElementCollector(doc)
//                .OfClass(typeof(FamilyInstance))
//                .OfCategory(BuiltInCategory.OST_StructuralFraming);

//            foreach (FamilyInstance beam in collector)
//            {
//                if (beam.StructuralType == StructuralType.Beam)
//                {
//                    var locationCurve = beam.Location as LocationCurve;
//                    if (locationCurve != null && locationCurve.Curve is Line line)
//                    {
//                        var beamInfo = new ExistingBeamInfo
//                        {
//                            BeamInstance = beam,
//                            StartPoint = line.GetEndPoint(0),
//                            EndPoint = line.GetEndPoint(1),
//                            TypeName = beam.Symbol.Name
//                        };

//                        try
//                        {
//                            var widthParam = beam.Symbol.LookupParameter("b") ??
//                                            beam.Symbol.LookupParameter("Width") ??
//                                            beam.Symbol.LookupParameter("Beam Width") ??
//                                            beam.Symbol.LookupParameter("w");

//                            var depthParam = beam.Symbol.LookupParameter("h") ??
//                                            beam.Symbol.LookupParameter("Depth") ??
//                                            beam.Symbol.LookupParameter("Beam Depth") ??
//                                            beam.Symbol.LookupParameter("d");

//                            if (widthParam != null && depthParam != null)
//                            {
//                                beamInfo.Width = widthParam.AsDouble() * 304.8; // Convert to mm
//                                beamInfo.Depth = depthParam.AsDouble() * 304.8; // Convert to mm
//                            }
//                        }
//                        catch
//                        {
//                            // Ignore parameter errors
//                        }

//                        existingBeams.Add(beamInfo);
//                    }
//                }
//            }
//            return existingBeams;
//        }

//        // CheckBeamDuplicates: Checks if a new beam conflicts with existing beams based on location and dimensions
//        private DuplicateCheckResult<ExistingBeamInfo> CheckBeamDuplicates(List<ExistingBeamInfo> existingBeams, XYZ startPoint, XYZ endPoint, BeamRCData beam)
//        {
//            var duplicateResult = new DuplicateCheckResult<ExistingBeamInfo>();

//            foreach (var existing in existingBeams)
//            {
//                bool sameLocation =
//                    (ArePointsClose(startPoint, existing.StartPoint, PositionTolerance) &&
//                     ArePointsClose(endPoint, existing.EndPoint, PositionTolerance)) ||
//                    (ArePointsClose(startPoint, existing.EndPoint, PositionTolerance) &&
//                     ArePointsClose(endPoint, existing.StartPoint, PositionTolerance));

//                if (sameLocation)
//                {
//                    bool dimensionsMatch =
//                        Math.Abs(beam.Width - existing.Width) < DimensionTolerance &&
//                        Math.Abs(beam.Depth - existing.Depth) < DimensionTolerance;

//                    if (dimensionsMatch)
//                    {
//                        duplicateResult.HasExactDuplicate = true;
//                        return duplicateResult;
//                    }
//                    duplicateResult.HasLocationConflict = true;
//                    duplicateResult.ConflictingElements.Add(existing);
//                }
//            }
//            return duplicateResult;
//        }

//        // GetOrCreateBeamType: Retrieves an existing beam type or uses a fallback if the specified type is not found
//        private FamilySymbol GetOrCreateBeamType(Document doc, BeamRCData beam)
//        {
//            var collector = new FilteredElementCollector(doc)
//                .OfClass(typeof(FamilySymbol))
//                .OfCategory(BuiltInCategory.OST_StructuralFraming);

//            foreach (FamilySymbol symbol in collector)
//            {
//                if (symbol.Name.Equals(beam.SectionName, StringComparison.OrdinalIgnoreCase))
//                    return symbol;
//            }

//            var fallbackSymbol = collector.Cast<FamilySymbol>().FirstOrDefault();
//            if (fallbackSymbol != null)
//            {
//                TaskDialog.Show("Warning", $"Beam type '{beam.SectionName}' not found. Using '{fallbackSymbol.Name}' instead.");
//                return fallbackSymbol;
//            }

//            return null;
//        }

//        // ShowLocationConflictDialog: Displays a dialog to resolve conflicts between new and existing beams
//        private DuplicateAction ShowLocationConflictDialog(BeamRCData newItem, List<ExistingBeamInfo> conflictingItems)
//        {
//            string conflictInfo = $"Beam '{newItem.SectionName}' ({newItem.Width:F0}×{newItem.Depth:F0}mm) at same location as:\n" +
//                                 string.Join("\n", conflictingItems.Select(i => $"• Existing beam '{i.TypeName}' ({i.Width:F0}×{i.Depth:F0}mm)"));

//            conflictInfo += "\nWhat would you like to do?";

//            var dialog = new TaskDialog("Beam Location Conflict")
//            {
//                MainInstruction = "Existing beam at same location",
//                MainContent = conflictInfo,
//                CommonButtons = TaskDialogCommonButtons.None
//            };

//            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Create both", "Keep existing beam and create new one");
//            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Replace", "Delete existing beam and create new one");
//            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "Skip", "Keep existing beam and skip new one");

//            dialog.DefaultButton = TaskDialogResult.CommandLink3;

//            TaskDialogResult dialogResult = dialog.Show();

//            return dialogResult switch
//            {
//                TaskDialogResult.CommandLink1 => DuplicateAction.CreateBoth,
//                TaskDialogResult.CommandLink2 => DuplicateAction.Replace,
//                _ => DuplicateAction.Skip
//            };
//        }

//        // ShowFinalSummary: Displays a summary of the number of beams created, skipped, and replaced
//        private void ShowFinalSummary(string elementType, int created, int skipped, int replaced)
//        {
//            string summary = $"{elementType} processing completed:\n\n" +
//                            $"Created: {created}\n" +
//                            $"Skipped: {skipped}\n" +
//                            $"Replaced: {replaced}\n\n" +
//                            $"Total: {created + skipped + replaced}";
//            TaskDialog.Show("Completed", summary);
//        }

//        // GetClosestLevel: Finds the closest level in the document based on elevation for beam placement
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