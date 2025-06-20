using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using ExportJsonFileFromRevit.Models;
using ElementsData.Steel;
using ElementsData.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ExportJsonFileFromRevit.Services
{
    // Implements Revit operations for creating structural elements from JSON data
    public class RevitService : IRevitService
    {
        private const double PositionTolerance = 0.01; // in feet (~3mm) - Used for spatial comparison tolerance
        private const double DimensionTolerance = 10.0; // in mm - Used for dimensional comparison tolerance

        // Combines beam and column data into a single FrameRCData object for unified processing
        public FrameRCData ProcessFrame(Document doc)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            FrameRCData frameData = new FrameRCData();
            ProcessBeams(doc, frameData.beamRCDatas);
            ProcessColumns(doc, frameData.columnRCDatas);
            return frameData;
        }

        // Creates beams in the Revit document based on JSON data, handling duplicates and conflicts
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

                        var duplicateResult = CheckBeamDuplicates(existingBeams, startPoint, endPoint, beam); // Check for duplicates or conflicting beams
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
                            return;
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

        // Creates columns in the Revit document based on JSON data, handling duplicates and conflicts
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
                            TaskDialog.Show("Error", $"Cannot find or create column type '{column.SymbolName}'.");
                            skipped++;
                            continue;
                        }

                        if (!symbol.IsActive) symbol.Activate();

                        Line columnLine = Line.CreateBound(basePoint, topPoint);
                        Level baseLevel = GetClosestLevel(doc, basePoint.Z); // Find the closest level for placement

                        if (baseLevel == null)
                        {
                            TaskDialog.Show("Error", "No levels found in document.");
                            return;
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
                            TypeName = column.SymbolName
                        });

                        created++;
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Error", $"Failed to create column '{column.SymbolName}': {ex.Message}");
                        skipped++;
                    }
                }

                tx.Commit();
                ShowFinalSummary("Columns", created, skipped, replaced); // Display summary of processing results
            }
        }

        // Creates walls in the Revit document based on JSON data, handling duplicates and conflicts
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
                        XYZ startPoint = new XYZ(wall.StartPoint.X / 304.8, wall.StartPoint.Y / 304.8, wall.StartPoint.Z / 304.8);
                        XYZ endPoint = new XYZ(wall.EndPoint.X / 304.8, wall.EndPoint.Y / 304.8, wall.EndPoint.Z / 304.8);

                        // Convert thickness and height from meters to mm for consistency
                        double thicknessInMm = wall.Thickness ;
                        double heightInMm = wall.Height;

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

        // Creates slabs in the Revit document based on JSON data, handling duplicates and conflicts
        public void ProcessSlabs(Document doc, List<SlabData> slabs)
        {
            if (slabs == null || slabs.Count == 0)
            {
                TaskDialog.Show("Warning", "No slab data found.");
                return;
            }

            // Retrieve existing slabs to check for duplicates
            var existingSlabs = GetExistingSlabs(doc);

            // Start a transaction to create slabs
            using (Transaction tx = new Transaction(doc, "Create Slabs"))
            {
                tx.Start();

                int created = 0, skipped = 0, replaced = 0;

                foreach (var slab in slabs)
                {
                    try
                    {
                        // Convert boundary points from millimeters (PointData) to feet (Revit internal units)
                        List<XYZ> boundaryPoints = slab.OuterBoundary.Select(p => new XYZ(
                            p.X / 304.8,
                            p.Y / 304.8,
                            p.Z / 304.8
                        )).ToList();

                        // Filter points to remove duplicates or very close points
                        List<XYZ> filteredBoundaryPoints = FilterPoints(boundaryPoints);

                        // Simplify boundary points to match Revit's behavior (remove collinear points)
                        List<XYZ> simplifiedBoundaryPoints = SimplifyBoundaryPoints(filteredBoundaryPoints);

                        // Calculate centroid, area, and bounding box for duplicate checking
                        XYZ newSlabCentroid = CalculateCentroid(simplifiedBoundaryPoints);
                        double newSlabArea = CalculatePolygonArea(simplifiedBoundaryPoints);
                        BoundingBoxXYZ newBoundingBox = CalculateBoundingBox(simplifiedBoundaryPoints);

                        // Check for duplicates using multiple criteria (centroid, area, bounding box)
                        var duplicateResult = CheckForDuplicatesAdvanced(existingSlabs, simplifiedBoundaryPoints,
                            newSlabCentroid, newSlabArea, newBoundingBox, slab);

                        if (duplicateResult.HasExactDuplicate)
                        {
                            skipped++;
                            continue;
                        }
                        else if (duplicateResult.HasLocationConflict)
                        {
                            // Prompt user to resolve location conflicts
                            var userChoice = ShowLocationConflictDialog(slab, duplicateResult.ConflictingElements);
                            if (userChoice == DuplicateAction.Skip)
                            {
                                skipped++;
                                continue;
                            }
                            else if (userChoice == DuplicateAction.Replace)
                            {
                                foreach (var existingSlab in duplicateResult.ConflictingElements)
                                {
                                    doc.Delete(existingSlab.SlabInstance.Id);
                                    existingSlabs.Remove(existingSlab);
                                }
                                replaced++;
                            }
                        }

                        // Get or create the floor type for the slab
                        FloorType floorType = GetOrCreateFloorType(doc, slab);
                        if (floorType == null)
                        {
                            TaskDialog.Show("Error", $"Cannot find or create floor type '{slab.SectionName}'.");
                            skipped++;
                            continue;
                        }

                        // Find the level by name or closest elevation
                        Level level = GetLevelByName(doc, slab.Level) ?? GetClosestLevel(doc, boundaryPoints[0].Z);
                        if (level == null)
                        {
                            TaskDialog.Show("Error", "No levels found in document.");
                            return;
                        }

                        // Create boundary curve array for the slab
                        CurveArray boundaryArray = new CurveArray();
                        for (int i = 0; i < simplifiedBoundaryPoints.Count; i++)
                        {
                            int nextIndex = (i + 1) % simplifiedBoundaryPoints.Count;
                            Line line = Line.CreateBound(simplifiedBoundaryPoints[i], simplifiedBoundaryPoints[nextIndex]);
                            boundaryArray.Append(line);
                        }

                        // Create the slab (floor) in Revit
                        Floor floor = doc.Create.NewFloor(boundaryArray, floorType, level, true);

                        // Handle openings if present
                        if (slab.Openings != null && slab.Openings.Count > 0)
                        {
                            foreach (var opening in slab.Openings)
                            {
                                try
                                {
                                    // Convert opening points from millimeters to feet
                                    List<XYZ> openingPoints = opening.Select(p => new XYZ(
                                        p.X / 304.8,
                                        p.Y / 304.8,
                                        p.Z / 304.8
                                    )).ToList();

                                    // Filter and simplify opening points
                                    List<XYZ> filteredOpeningPoints = FilterPoints(openingPoints);
                                    List<XYZ> simplifiedOpeningPoints = SimplifyBoundaryPoints(filteredOpeningPoints);

                                    // Create opening curve array
                                    CurveArray openingArray = new CurveArray();
                                    for (int i = 0; i < simplifiedOpeningPoints.Count; i++)
                                    {
                                        int nextIndex = (i + 1) % simplifiedOpeningPoints.Count;
                                        Line line = Line.CreateBound(simplifiedOpeningPoints[i], simplifiedOpeningPoints[nextIndex]);
                                        openingArray.Append(line);
                                    }

                                    // Create the opening in the slab
                                    doc.Create.NewOpening(floor, openingArray, true);
                                }
                                catch (Exception ex)
                                {
                                    TaskDialog.Show("Warning", $"Failed to create opening in slab: {ex.Message}");
                                }
                            }
                        }

                        // Set the slab thickness if different from the floor type default
                        try
                        {
                            Parameter thicknessParam = floor.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM);
                            if (thicknessParam != null && !thicknessParam.IsReadOnly)
                            {
                                thicknessParam.Set(slab.Thickness / 304.8); // Convert mm to feet
                            }
                        }
                        catch
                        {
                            // Ignore thickness setting errors
                        }

                        // Track the new slab for future duplicate checks
                        existingSlabs.Add(new ExistingSlabInfo
                        {
                            SlabInstance = floor,
                            BoundaryPoints = boundaryPoints,
                            FilteredBoundaryPoints = filteredBoundaryPoints,
                            SimplifiedBoundaryPoints = simplifiedBoundaryPoints,
                            Centroid = CalculateCentroid(boundaryPoints),
                            FilteredCentroid = CalculateCentroid(filteredBoundaryPoints),
                            SimplifiedCentroid = newSlabCentroid,
                            Thickness = slab.Thickness,
                            TypeName = floorType.Name,
                            LevelName = level.Name,
                            Elevation = simplifiedBoundaryPoints[0].Z,
                            Area = newSlabArea,
                            BoundingBox = newBoundingBox
                        });

                        created++;
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Error", $"Failed to create slab '{slab.SectionName}': {ex.Message}");
                        skipped++;
                    }
                }

                tx.Commit();
                ShowFinalSummary("Slabs", created, skipped, replaced); // Display summary of processing results
            }
        }

        // Placeholder: Process beam geometry data if additional geometry processing is required
        public void ProcessBeamsGeometry(Document doc, List<BeamGeometryData> beams)
        {
            // Implementation pending
        }

        // Placeholder: Process column geometry data if additional geometry processing is required
        public void ProcessColumnsGeometry(Document doc, List<ColumnGeometryData> columns)
        {
            // Implementation pending
        }

        // Retrieves all existing beams from the document to check for duplicates
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

        // Retrieves all existing columns from the document to check for duplicates
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

        // Retrieves all existing walls from the document to check for duplicates
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

        // Retrieves all existing slabs from the document to check for duplicates
        private List<ExistingSlabInfo> GetExistingSlabs(Document doc)
        {
            var existingSlabs = new List<ExistingSlabInfo>();

            // Collect all floor elements in the document
            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfClass(typeof(Floor));

            foreach (Floor floor in collector)
            {
                try
                {
                    // Get the sketch defining the floor's boundary
                    var sketch = floor.GetDependentElements(new ElementClassFilter(typeof(Sketch))).FirstOrDefault();
                    if (sketch != null)
                    {
                        var sketchElement = doc.GetElement(sketch) as Sketch;
                        if (sketchElement != null)
                        {
                            var profile = sketchElement.Profile;
                            var boundaryPoints = new List<XYZ>();

                            // Extract boundary points from the sketch profile
                            foreach (CurveArray curveArray in profile)
                            {
                                foreach (Curve curve in curveArray)
                                {
                                    boundaryPoints.Add(curve.GetEndPoint(0));
                                }
                            }

                            // Filter and simplify boundary points
                            var filteredBoundaryPoints = FilterPoints(boundaryPoints);
                            var simplifiedBoundaryPoints = SimplifyBoundaryPoints(filteredBoundaryPoints);

                            // Calculate centroids for different point sets
                            XYZ centroid = CalculateCentroid(boundaryPoints);
                            XYZ filteredCentroid = CalculateCentroid(filteredBoundaryPoints);
                            XYZ simplifiedCentroid = CalculateCentroid(simplifiedBoundaryPoints);

                            // Calculate area and bounding box
                            double area = CalculatePolygonArea(simplifiedBoundaryPoints);
                            BoundingBoxXYZ boundingBox = CalculateBoundingBox(simplifiedBoundaryPoints);

                            var slabInfo = new ExistingSlabInfo
                            {
                                SlabInstance = floor,
                                BoundaryPoints = boundaryPoints,
                                FilteredBoundaryPoints = filteredBoundaryPoints,
                                SimplifiedBoundaryPoints = simplifiedBoundaryPoints,
                                Centroid = centroid,
                                FilteredCentroid = filteredCentroid,
                                SimplifiedCentroid = simplifiedCentroid,
                                TypeName = floor.FloorType.Name,
                                LevelName = floor.LevelId != ElementId.InvalidElementId ?
                                           doc.GetElement(floor.LevelId).Name : "Unknown",
                                Area = area,
                                BoundingBox = boundingBox
                            };

                            // Get slab thickness
                            try
                            {
                                Parameter thicknessParam = floor.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM);
                                if (thicknessParam != null)
                                {
                                    slabInfo.Thickness = thicknessParam.AsDouble() * 304.8; // Convert to mm
                                }
                            }
                            catch
                            {
                                // Ignore parameter errors
                            }

                            // Set elevation from simplified points
                            if (simplifiedBoundaryPoints.Count > 0)
                            {
                                slabInfo.Elevation = simplifiedBoundaryPoints[0].Z;
                            }

                            existingSlabs.Add(slabInfo);
                        }
                    }
                }
                catch
                {
                    // Skip floors that can't be processed
                }
            }

            return existingSlabs;
        }

        // Checks if a new beam conflicts with existing beams based on location and dimensions
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

        // Checks if a new column conflicts with existing columns based on location and dimensions
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
                        Math.Abs(basePoint.Z - existing.BasePoint.Z) < PositionTolerance;

                    bool sameTopLocation =
                        Math.Abs(topPoint.X - existing.TopPoint.X) < PositionTolerance &&
                        Math.Abs(topPoint.Y - existing.TopPoint.Y) < PositionTolerance &&
                        Math.Abs(topPoint.Z - existing.TopPoint.Z) < PositionTolerance;

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

        // Checks if a new wall conflicts with existing walls based on location and dimensions
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

        // Checks for duplicate slabs using multiple criteria (centroid, area, bounding box, thickness)
        private DuplicateCheckResult<ExistingSlabInfo> CheckForDuplicatesAdvanced(List<ExistingSlabInfo> existingSlabs,
            List<XYZ> newSimplifiedPoints, XYZ newSlabCentroid, double newSlabArea, BoundingBoxXYZ newBoundingBox, SlabData newSlab)
        {
            var result = new DuplicateCheckResult<ExistingSlabInfo>();
            const double centroidTolerance = 0.1; // 10 cm in feet
            const double areaTolerance = 0.01; // 1% of area
            const double thicknessTolerance = 10.0; // in mm
            const double boundingBoxTolerance = 0.1; // in feet

            foreach (var existing in existingSlabs)
            {
                // Check if geometry matches (centroid, area, bounding box)
                bool geometryMatch = ArePointsClose(newSlabCentroid, existing.SimplifiedCentroid, centroidTolerance) &&
                                    Math.Abs(newSlabArea - existing.Area) < (Math.Max(newSlabArea, existing.Area) * areaTolerance) &&
                                    AreBoundingBoxesSimilar(newBoundingBox, existing.BoundingBox, boundingBoxTolerance);

                if (geometryMatch)
                {
                    // Check if thickness matches
                    bool thicknessMatch = Math.Abs(newSlab.Thickness - existing.Thickness) < thicknessTolerance;
                    if (thicknessMatch)
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

        // Simplifies boundary points by removing collinear points, mimicking Revit’s behavior
        private List<XYZ> SimplifyBoundaryPoints(List<XYZ> points)
        {
            if (points == null || points.Count <= 3)
                return points;

            var simplifiedPoints = new List<XYZ>();
            const double collinearityTolerance = 0.001; // Tolerance for collinear points

            for (int i = 0; i < points.Count; i++)
            {
                XYZ currentPoint = points[i];
                XYZ prevPoint = points[(i - 1 + points.Count) % points.Count];
                XYZ nextPoint = points[(i + 1) % points.Count];

                // Keep point if it’s not collinear with neighbors
                if (!ArePointsCollinear(prevPoint, currentPoint, nextPoint, collinearityTolerance))
                {
                    simplifiedPoints.Add(currentPoint);
                }
            }

            // Ensure at least 3 points for a valid polygon
            return simplifiedPoints.Count < 3 ? points : simplifiedPoints;
        }

        // Checks if three points are collinear within a tolerance
        private bool ArePointsCollinear(XYZ p1, XYZ p2, XYZ p3, double tolerance)
        {
            // Calculate triangle area to check collinearity
            double area = Math.Abs((p2.X - p1.X) * (p3.Y - p1.Y) - (p3.X - p1.X) * (p2.Y - p1.Y)) / 2.0;
            return area < tolerance;
        }

        // Calculates the area of a polygon defined by its vertices
        private double CalculatePolygonArea(List<XYZ> points)
        {
            if (points == null || points.Count < 3)
                return 0;

            double area = 0;
            for (int i = 0; i < points.Count; i++)
            {
                int j = (i + 1) % points.Count;
                area += points[i].X * points[j].Y;
                area -= points[j].X * points[i].Y;
            }
            return Math.Abs(area) / 2.0;
        }

        // Calculates the bounding box for a set of points
        private BoundingBoxXYZ CalculateBoundingBox(List<XYZ> points)
        {
            if (points == null || points.Count == 0)
                return null;

            double minX = points.Min(p => p.X);
            double maxX = points.Max(p => p.X);
            double minY = points.Min(p => p.Y);
            double maxY = points.Max(p => p.Y);
            double minZ = points.Min(p => p.Z);
            double maxZ = points.Max(p => p.Z);

            BoundingBoxXYZ bbox = new BoundingBoxXYZ();
            bbox.Min = new XYZ(minX, minY, minZ);
            bbox.Max = new XYZ(maxX, maxY, maxZ);
            return bbox;
        }

        // Filters points to remove duplicates or points closer than tolerance
        private List<XYZ> FilterPoints(List<XYZ> points)
        {
            if (points == null || points.Count == 0)
                return new List<XYZ>();

            var filteredPoints = new List<XYZ>();
            const double tolerance = 0.01; // ~1 cm in feet

            foreach (var point in points)
            {
                bool isDuplicate = filteredPoints.Any(p => ArePointsClose(point, p, tolerance));
                if (!isDuplicate)
                {
                    filteredPoints.Add(point);
                }
            }

            // Ensure at least 3 points for a valid polygon
            return filteredPoints.Count < 3 ? points : filteredPoints;
        }

        // Compares two points to determine if they are within tolerance
        private bool ArePointsClose(XYZ point1, XYZ point2, double tolerance)
        {
            return Math.Abs(point1.X - point2.X) < tolerance &&
                   Math.Abs(point1.Y - point2.Y) < tolerance &&
                   Math.Abs(point1.Z - point2.Z) < tolerance;
        }

        // Calculates the centroid of a set of points
        private XYZ CalculateCentroid(List<XYZ> boundaryPoints)
        {
            if (boundaryPoints == null || boundaryPoints.Count == 0)
                return new XYZ(0, 0, 0);

            double sumX = 0, sumY = 0, sumZ = 0;
            foreach (var point in boundaryPoints)
            {
                sumX += point.X;
                sumY += point.Y;
                sumZ += point.Z;
            }

            return new XYZ(
                sumX / boundaryPoints.Count,
                sumY / boundaryPoints.Count,
                sumZ / boundaryPoints.Count);
        }

        // Compares two bounding boxes to check if they are similar within tolerance
        private bool AreBoundingBoxesSimilar(BoundingBoxXYZ bbox1, BoundingBoxXYZ bbox2, double tolerance)
        {
            if (bbox1 == null || bbox2 == null)
                return false;

            return ArePointsClose(bbox1.Min, bbox2.Min, tolerance) &&
                   ArePointsClose(bbox1.Max, bbox2.Max, tolerance);
        }

        // Displays a dialog to resolve conflicts between new and existing elements
        private DuplicateAction ShowLocationConflictDialog<T1, T2>(T1 newItem, List<T2> conflictingItems)
        {
            string conflictInfo = newItem switch
            {
                BeamRCData beam => $"Beam '{beam.SectionName}' ({beam.Width:F0}×{beam.Depth:F0}mm) at same location as:\n" +
                    string.Join("\n", conflictingItems.Select(i => i is ExistingBeamInfo bi ? $"• Existing beam '{bi.TypeName}' ({bi.Width:F0}×{bi.Depth:F0}mm)" : "")),
                ColumnRCData col => $"Column '{col.SymbolName}' ({col.Width:F0}×{col.Depth:F0}mm) at same location as:\n" +
                    string.Join("\n", conflictingItems.Select(i => i is ExistingColumnInfo ci ? $"• Existing column '{ci.TypeName}' ({ci.Width:F0}×{ci.Depth:F0}mm)" : "")),
                WallRCData wall => $"Wall '{wall.Name}' ({(wall.Thickness * 1000):F0}mm thick, {(wall.Height * 1000):F0}mm high) at same location as:\n" +
                    string.Join("\n", conflictingItems.Select(i => i is ExistingWallInfo wi ? $"• Existing wall '{wi.WallName ?? wi.TypeName}' ({wi.Thickness:F0}mm thick, {wi.Height:F0}mm high)" : "")),
                SlabData slab => $"Slab '{slab.SectionName}' (thickness: {slab.Thickness:F0}mm) at same location as:\n" +
                    string.Join("\n", conflictingItems.Select(i => i is ExistingSlabInfo si ? $"• Existing slab '{si.TypeName}' (thickness: {si.Thickness:F0}mm)" : "")),
                _ => "Unknown element conflict"
            };

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

        // Displays a summary of the number of elements created, skipped, and replaced
        private void ShowFinalSummary(string elementType, int created, int skipped, int replaced)
        {
            string summary = $"{elementType} processing completed:\n\n" +
                            $"Created: {created}\n" +
                            $"Skipped: {skipped}\n" +
                            $"Replaced: {replaced}\n\n" +
                            $"Total: {created + skipped + replaced}";
            TaskDialog.Show("Completed", summary);
        }

        // Retrieves an existing beam type or uses a fallback if the specified type is not found
        private FamilySymbol GetOrCreateBeamType(Document doc, BeamRCData beam)
        {
            var collector = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_StructuralFraming);

            foreach (FamilySymbol symbol in collector)
            {
                if (symbol.Name.Equals($"{beam.Width:F0} x {beam.Depth:F0}mm"))
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

        // Retrieves an existing column type or uses a fallback if the specified type is not found
        private FamilySymbol GetOrCreateColumnType(Document doc, ColumnRCData column)
        {
            var collector = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_StructuralColumns);

            foreach (FamilySymbol symbol in collector)
            {
                if (symbol.Name.Equals(column.SectionName))
                    return symbol;
            }

            var fallbackSymbol = collector.Cast<FamilySymbol>().FirstOrDefault();
            if (fallbackSymbol != null)
            {
                TaskDialog.Show("Warning", $"Column type '{column.SymbolName}' not found. Using '{fallbackSymbol.Name}' instead.");
                return fallbackSymbol;
            }

            return null;
        }

        // Retrieves an existing wall type or uses a fallback if the specified type is not found
        private WallType GetOrCreateWallType(Document doc, WallRCData wall)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(WallType));
            foreach (WallType wallType in collector)
            {
                if (wallType.Name.Equals(wall.WallTypeName, StringComparison.OrdinalIgnoreCase) ||
                    wallType.Name.Equals(wall.Name, StringComparison.OrdinalIgnoreCase))
                    return wallType;
            }

            WallType fallbackType = collector.Cast<WallType>().FirstOrDefault(wt => wt.Kind == WallKind.Basic);
            if (fallbackType != null)
            {
                TaskDialog.Show("Warning", $"Wall type '{wall.WallTypeName}' not found. Using '{fallbackType.Name}' instead.");
                return fallbackType;
            }

            return null;
        }

        // Retrieves an existing floor type or uses a fallback if the specified type is not found
        private FloorType GetOrCreateFloorType(Document doc, SlabData slab)
        {
            // Collect all floor types in the document
            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfClass(typeof(FloorType));

            // Find matching floor type by name
            foreach (FloorType floorType in collector)
            {
                if (floorType.Name.Equals(slab.SectionName, StringComparison.OrdinalIgnoreCase))
                    return floorType;
            }

            // Use fallback if no match found
            FloorType fallbackType = collector.Cast<FloorType>().FirstOrDefault();
            if (fallbackType != null)
            {
                TaskDialog.Show("Warning", $"Floor type '{slab.SectionName}' not found. Using '{fallbackType.Name}' instead.");
                return fallbackType;
            }

            return null;
        }

        // Finds a level by name
        private Level GetLevelByName(Document doc, string levelName)
        {
            // Collect all levels in the document
            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfClass(typeof(Level));

            // Find level by name (case-insensitive)
            foreach (Level level in collector)
            {
                if (level.Name.Equals(levelName, StringComparison.OrdinalIgnoreCase))
                    return level;
            }

            return null;
        }

        // Finds the closest level to a given elevation
        private Level GetClosestLevel(Document doc, double elevation)
        {
            Level closest = null;
            double minDiff = double.MaxValue;

            // Collect all levels in the document
            var levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>();

            // Find level with minimum elevation difference
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