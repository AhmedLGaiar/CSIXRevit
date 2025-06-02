using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using ElementsData.Geometry;
using GeometryToRevit.ExistingInfo;
using GeometryToRevit.Utilities;
using Newtonsoft.Json;
using System.IO;

namespace ExportJsonFileFromRevit
{
    [Transaction(TransactionMode.Manual)]
    public class DrawBeam : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var doc = commandData.Application.ActiveUIDocument.Document;
            string jsonPath = @"C:\Users\Mahmoud\Desktop\ETABS_Beams_Geometry.json";

            if (!File.Exists(jsonPath))
            {
                TaskDialog.Show("Error", "JSON file not found.");
                return Result.Failed;
            }

            List<BeamGeometryData> beams;
            try
            {
                beams = JsonConvert.DeserializeObject<List<BeamGeometryData>>(File.ReadAllText(jsonPath));
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Failed to parse JSON file: {ex.Message}");
                return Result.Failed;
            }

            if (beams == null || beams.Count == 0)
            {
                TaskDialog.Show("Warning", "No beam data found in JSON file.");
                return Result.Succeeded;
            }

            // Get all existing beams in the document
            List<ExistingBeamInfo> existingBeams = GetExistingBeams(doc);

            using (Transaction tx = new Transaction(doc, "Create Beams"))
            {
                tx.Start();

                int created = 0;
                int skipped = 0;
                int replaced = 0;

                foreach (var beam in beams)
                {
                    try
                    {
                        XYZ start = new XYZ(
                             UnitUtils.ConvertToInternalUnits(beam.StartPoint.X, UnitTypeId.Millimeters),
                             UnitUtils.ConvertToInternalUnits(beam.StartPoint.Y, UnitTypeId.Millimeters),
                             UnitUtils.ConvertToInternalUnits(beam.StartPoint.Z, UnitTypeId.Millimeters)
                        );

                        XYZ end = new XYZ(
                            UnitUtils.ConvertToInternalUnits(beam.EndPoint.X, UnitTypeId.Millimeters),
                            UnitUtils.ConvertToInternalUnits(beam.EndPoint.Y, UnitTypeId.Millimeters),
                            UnitUtils.ConvertToInternalUnits(beam.EndPoint.Z, UnitTypeId.Millimeters)
                        );


                        // Check for duplicates
                        var duplicateResult = CheckForDuplicates(existingBeams, start, end, beam);

                        if (duplicateResult.HasExactDuplicate)
                        {
                            skipped++;
                            continue; // Skip creating beam, keeping existing beam unchanged
                        }
                        else if (duplicateResult.HasLocationConflict)
                        {
                            // Show user choice dialog for location conflicts (different dimensions)
                            var userChoice = ShowDuplicateChoiceDialog(beam, duplicateResult.ConflictingBeams);

                            if (userChoice == DuplicateAction.Skip)
                            {
                                skipped++;
                                continue;
                            }
                            else if (userChoice == DuplicateAction.Replace)
                            {
                                // Delete existing beams at this location
                                foreach (var existingBeam in duplicateResult.ConflictingBeams)
                                {
                                    doc.Delete(existingBeam.BeamInstance.Id);
                                }
                                // Remove from our tracking list
                                foreach (var toRemove in duplicateResult.ConflictingBeams)
                                {
                                    existingBeams.Remove(toRemove);
                                }
                                replaced++;
                            }
                            // If userChoice == DuplicateAction.CreateBoth, continue with creation
                        }

                        // Create the new beam if no exact duplicate or if user chose to create
                        FamilySymbol symbol = GetOrCreateBeamType(doc, beam);
                        if (symbol == null)
                        {
                            TaskDialog.Show("Error", $"Could not find or create beam type '{beam.Name}'. Skipping this beam.");
                            skipped++;
                            continue;
                        }

                        if (!symbol.IsActive)
                            symbol.Activate();

                        Line line = Line.CreateBound(start, end);
                        Level level = GetClosestLevel(doc, start.Z);

                        if (level == null)
                        {
                            TaskDialog.Show("Error", "No levels found in the document. Cannot create beams.");
                            break;
                        }

                        FamilyInstance beamInstance = doc.Create.NewFamilyInstance(line, symbol, level, StructuralType.Beam);

                        // Add to existing beams list for future duplicate checking
                        existingBeams.Add(new ExistingBeamInfo
                        {
                            BeamInstance = beamInstance,
                            StartPoint = start,
                            EndPoint = end,
                            Width = beam.Width, // Convert to feet for consistency
                            Depth = beam.Depth, // Convert to feet for consistency
                            TypeName = beam.Name
                        });

                        created++;
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Error", $"Failed to create beam '{beam.Name}': {ex.Message}");
                        skipped++;
                    }
                }

                tx.Commit();

                // Show summary
                string summary = $"Beam processing completed:\n" +
                               $"Created: {created}\n" +
                               $"Skipped: {skipped}\n" +
                               $"Replaced: {replaced}";

                TaskDialog.Show("Done", summary);
            }

            return Result.Succeeded;
        }

        private List<ExistingBeamInfo> GetExistingBeams(Document doc)
        {
            var existingBeams = new List<ExistingBeamInfo>();

            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_StructuralFraming);

            foreach (FamilyInstance beam in collector)
            {
                if (beam.StructuralType == StructuralType.Beam)
                {
                    // Get beam geometry
                    var curve = (beam.Location as LocationCurve)?.Curve;
                    if (curve != null)
                    {
                        var beamInfo = new ExistingBeamInfo
                        {
                            BeamInstance = beam,
                            StartPoint = curve.GetEndPoint(0),
                            EndPoint = curve.GetEndPoint(1),
                            TypeName = beam.Symbol.Name
                        };

                        // Try to get dimensions from parameters
                        try
                        {
                            Parameter widthParam = beam.Symbol.LookupParameter("b") ??
                                          beam.Symbol.LookupParameter("Width") ??
                                          beam.Symbol.LookupParameter("Beam Width") ??
                                          beam.Symbol.LookupParameter("w");

                            Parameter heightParam = beam.Symbol.LookupParameter("h") ??
                                                  beam.Symbol.LookupParameter("Height") ??
                                                  beam.Symbol.LookupParameter("Beam Depth") ??
                                                  beam.Symbol.LookupParameter("d") ??
                                                  beam.Symbol.LookupParameter("Depth");

                            beamInfo.Width = widthParam?.AsDouble() ?? 0.0;
                            beamInfo.Depth = heightParam?.AsDouble() ?? 0.0;
                        }
                        catch
                        {
                            beamInfo.Width = 0;
                            beamInfo.Depth = 0;
                        }

                        existingBeams.Add(beamInfo);
                    }
                }
            }

            return existingBeams;
        }

        private DuplicateCheckResult CheckForDuplicates(List<ExistingBeamInfo> existingBeams, XYZ newStart, XYZ newEnd, BeamGeometryData newBeam)
        {
            var result = new DuplicateCheckResult();
            const double positionTolerance = 0.01; // 1cm tolerance in feet
            const double dimensionTolerance = 1.0; // 1mm tolerance

            foreach (var existing in existingBeams)
            {
                // Check if locations are the same (within tolerance)
                bool sameLocation = (IsPointsEqual(existing.StartPoint, newStart, positionTolerance) &&
                                   IsPointsEqual(existing.EndPoint, newEnd, positionTolerance)) ||
                                   (IsPointsEqual(existing.StartPoint, newEnd, positionTolerance) &&
                                   IsPointsEqual(existing.EndPoint, newStart, positionTolerance));

                if (sameLocation)
                {
                    // Convert existing beam dimensions to mm for comparison
                    double existingWidthMM = existing.Width * 304.8;
                    double existingHeightMM = existing.Depth * 304.8;

                    // Check if dimensions match within tolerance
                    bool dimensionsMatch = Math.Abs(existingWidthMM - newBeam.Width) < dimensionTolerance &&
                                          Math.Abs(existingHeightMM - newBeam.Depth) < dimensionTolerance;

                    if (dimensionsMatch)
                    {
                        // Exact duplicate found
                        result.HasExactDuplicate = true;
                        return result; // Exit early
                    }
                    else
                    {
                        // Location conflict with different dimensions
                        result.HasLocationConflict = true;
                        result.ConflictingBeams.Add(existing);
                    }
                }
            }

            return result;
        }

        private bool IsPointsEqual(XYZ point1, XYZ point2, double tolerance)
        {
            return Math.Abs(point1.X - point2.X) < tolerance &&
                   Math.Abs(point1.Y - point2.Y) < tolerance &&
                   Math.Abs(point1.Z - point2.Z) < tolerance;
        }

        private DuplicateAction ShowDuplicateChoiceDialog(BeamGeometryData newBeam, List<ExistingBeamInfo> conflictingBeams)
        {
            string conflictInfo = $"New beam '{newBeam.Name}' ({newBeam.Width:F0}x{newBeam.Depth:F0}mm) conflicts with:\n\n";

            foreach (var existing in conflictingBeams)
            {
                double widthMM = existing.Width * 304.8;
                double heightMM = existing.Depth * 304.8;
                conflictInfo += $"• Existing beam '{existing.TypeName}' ({widthMM:F0}x{heightMM:F0}mm)\n";
            }

            conflictInfo += "\nWhat would you like to do?";

            TaskDialog dialog = new TaskDialog("Beam Conflict Detected")
            {
                MainInstruction = "Beam Location Conflict",
                MainContent = conflictInfo,
                CommonButtons = TaskDialogCommonButtons.None
            };

            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Create Both Beams", "Keep existing beam(s) and create the new one at the same location");
            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Replace Existing", "Delete existing beam(s) and create the new one");
            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "Skip New Beam", "Keep existing beam(s) and skip creating the new one");

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

        private FamilySymbol GetOrCreateBeamType(Document doc, BeamGeometryData beam)
        {
            // Try to find existing FamilySymbol
            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_StructuralFraming);

            foreach (FamilySymbol symbol in collector)
            {
                if (symbol.Name.Equals(beam.Name, StringComparison.OrdinalIgnoreCase))
                    return symbol;
            }

            // If not found, try to find any structural framing symbol as fallback
            FamilySymbol fallbackSymbol = collector.Cast<FamilySymbol>().FirstOrDefault();

            if (fallbackSymbol != null)
            {
                TaskDialog.Show("Warning", $"Beam type '{beam.Name}' not found. Using '{fallbackSymbol.Name}' instead.");
                return fallbackSymbol;
            }

            // No beam types found at all
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