using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using ElementsData.Geometry;
using EtabsGempetryExport.Model.HelperClasses;
using ETABSv1;

namespace EtabsGempetryExport.Model.Service
{
    public class ETABSService : IETABSService
    {
        private cSapModel _model;
        private cOAPI _etabsObject;
        private ETABSModelHelper _helper;

        public bool ConnectToETABS()
        {
            var (success, etabsObject, model) = ETABSHelper.GetActiveETABSInstance();

            if (success)
            {
                _etabsObject = etabsObject;
                _model = model;
                _helper = new ETABSModelHelper(_model);
            }

            return success;
        }

        public async Task<List<BeamGeometryData>> ExtractBeamsAsync()
        {
            return await Task.Run(ExtractBeams);
        }

        private List<BeamGeometryData> ExtractBeams()
        {
            ValidationHelper.EnsureModelConnected(_model);
            var beams = new List<BeamGeometryData>();

            try
            {
                var frameNames = _helper.GetAllFrameNames();

                foreach (string name in frameNames)
                {
                    var (label, story) = _helper.GetFrameInfo(name);

                    if (!FrameHelper.IsBeam(label))
                        continue;

                    var (startPointName, endPointName) = _helper.GetFramePoints(name);
                    var startCoord = FrameHelper.GetPointCoordinates(_model, startPointName);
                    var endCoord = FrameHelper.GetPointCoordinates(_model, endPointName);

                    var sectionName = _helper.GetFrameSection(name);
                    var (width, height) = _helper.GetSectionDimensions(sectionName); // Height here is treated as depth

                    beams.Add(new BeamGeometryData
                    {
                        ApplicationId = name,
                        Name = $"{width} x {height}mm",
                        StartPoint = new PointData
                        {
                            X = startCoord.X,
                            Y = startCoord.Y,
                            Z = startCoord.Z
                        },
                        EndPoint = new PointData
                        {
                            X = endCoord.X,
                            Y = endCoord.Y,
                            Z = endCoord.Z
                        },
                        Width = width,
                        Depth = height
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error extracting beams: {ex.Message}", ex);
            }

            return beams;
        }


        public async Task<List<SlabData>> ExtractSlapAsync()
        {
            return await Task.Run(ExtractSlabs);
        }

        private List<SlabData> ExtractSlabs()
        {
            ValidationHelper.EnsureModelConnected(_model);
            List<SlabData> slabs = new List<SlabData>();

            int numberNames = 0;
            string[] areaNames = new string[0];
            _model.AreaObj.GetNameList(ref numberNames, ref areaNames);

            foreach (var areaName in areaNames)
            {
                // Get label and story
                string label = "", story = "";
                _model.AreaObj.GetLabelFromName(areaName, ref label, ref story);

                // Use custom logic to check if this is a slab
                if (!FrameHelper.IsSlab(label))
                    continue;

                // Get section/property name
                string sectionName = "";
                _model.AreaObj.GetProperty(areaName, ref sectionName);

                // Get slab thickness and type
                var slabProps = FrameHelper.GetSlabProperties(_model, sectionName);
                double thickness = slabProps.thickness;
                string slabType = slabProps.slabType;

                // Get points of the slab
                int numPoints = 0;
                string[] pointNames = new string[0];
                _model.AreaObj.GetPoints(areaName, ref numPoints, ref pointNames);

                List<PointData> outerBoundary = new List<PointData>();
                double sumX = 0, sumY = 0, sumZ = 0;

                foreach (var point in pointNames)
                {
                    Point3D pt = FrameHelper.GetPointCoordinates(_model, point);
                    outerBoundary.Add(new PointData { X = pt.X, Y = pt.Y, Z = pt.Z });
                    sumX += pt.X;
                    sumY += pt.Y;
                    sumZ += pt.Z;
                }

                double centroidZ = (pointNames.Length > 0) ? (sumZ / pointNames.Length) : 0;

                slabs.Add(new SlabData
                {
                    
                    SectionName = $"Generic {thickness}mm",
                    Thickness = thickness,
                    Level = story,
                    OuterBoundary = outerBoundary,
                    Openings = new List<List<PointData>>()
                });
            }

            return slabs;
        }



        public async Task<List<ColumnGeometryData>> ExtractColumnsAsync()
        {
            return await Task.Run(ExtractColumns);
        }

        private List<ColumnGeometryData> ExtractColumns()
        {
            ValidationHelper.EnsureModelConnected(_model);
            var columns = new List<ColumnGeometryData>();

            try
            {
                var frameNames = _helper.GetAllFrameNames();

                foreach (string name in frameNames)
                {
                    var (label, story) = _helper.GetFrameInfo(name);

                    if (!FrameHelper.IsColumn(label))
                        continue;

                    var (point1, point2) = _helper.GetFramePoints(name);
                    var basePoint = FrameHelper.GetPointCoordinates(_model, point1);
                    var topPoint = FrameHelper.GetPointCoordinates(_model, point2);

                    var sectionName = _helper.GetFrameSection(name);
                    var (width, depth) = _helper.GetSectionDimensions(sectionName);
                    var rotation = _helper.GetFrameRotation(name);

                    var (baseLabel, baseStory) = _helper.GetPointInfo(point1);
                    var (topLabel, topStory) = _helper.GetPointInfo(point2);

                    columns.Add(new ColumnGeometryData
                    {
                        Id = name,
                        BasePoint = new PointData { X = basePoint.X, Y = basePoint.Y, Z = basePoint.Z},
                        TopPoint = new PointData { X = topPoint.X, Y = topPoint.Y, Z = topPoint.Z },
                        Width = depth,
                        Depth = width,
                        SectionName = $"{depth} x {width}mm",
                        Rotation = rotation,
                        SlantedAngle = _helper.GetDesignOrientation(name),
                        BaseLevel = baseStory,
                        TopLevel = topStory,
                        Story = story
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error extracting columns: {ex.Message}", ex);
            }

            return columns;
        }



        private List<StructuralWallData> ExtractWalls()
        {
            ValidationHelper.EnsureModelConnected(_model);
            List<StructuralWallData> walls = new List<StructuralWallData>();

            // Get all area object names
            int numberNames = 0;
            string[] areaNames = new string[0];
            _model.AreaObj.GetNameList(ref numberNames, ref areaNames);

            foreach (var areaName in areaNames)
            {
                // Get label to check if it's a wall
                string label = "", story = "";
                _model.AreaObj.GetLabelFromName(areaName, ref label, ref story);

                // Skip if not a wall
                if (!FrameHelper.IsWall(label))
                    continue;

                // Get section/property name
                string sectionName = "";
                _model.AreaObj.GetProperty(areaName, ref sectionName);

                // Get wall properties (thickness and material)
                var wallProps = FrameHelper.GetWallProperties(_model, sectionName);
                double thickness = wallProps.thickness;
                string material = wallProps.material;

                // Get points of the wall (should be 4 points for a rectangular wall)
                int numPoints = 0;
                string[] pointNames = new string[0];
                _model.AreaObj.GetPoints(areaName, ref numPoints, ref pointNames);

                if (pointNames.Length < 2)
                    continue; // Skip if insufficient points

                // Get coordinates for all points
                List<Point3D> wallPoints = new List<Point3D>();
                foreach (var pointName in pointNames)
                {
                    Point3D pt = FrameHelper.GetPointCoordinates(_model, pointName);
                    wallPoints.Add(pt);
                }

                // Calculate wall properties from points

                var wallGeometry = _helper.CalculateWallGeometry(wallPoints);

                // Get additional area properties
                string guid = "";
                _model.AreaObj.GetGUID(areaName, ref guid);

                // Create wall data object
                var wallData = new StructuralWallData
                {
                    Id = guid,
                    Name = areaName,
                    Section = $"Generic - {thickness}mm",
                    Story = story,
                    StartPoint = new PointData { X = wallGeometry.StartPoint.X, Y = wallGeometry.StartPoint.Y, Z = wallGeometry.StartPoint.Z },
                    EndPoint = new PointData { X = wallGeometry.EndPoint.X, Y = wallGeometry.EndPoint.Y, Z = wallGeometry.EndPoint.Z },
                    Length = wallGeometry.Length,
                    Thickness = thickness,
                    Height = wallGeometry.Height,
                    OrientationAngle = wallGeometry.OrientationAngle,
                    Orientation = wallGeometry.OrientationAngle * (180.0 / Math.PI), // Convert to degrees
                    BaseLevel = story, // You might need to get this differently based on your model
                    TopLevel = story,  // You might need to get this differently based on your model
                    Material = material,
                    WallTypeName = $"Generic - {thickness}mm",
                    AdditionalProperties = new Dictionary<string, string>
                    {
                        ["Label"] = label,
                        ["GUID"] = guid,
                        ["NumPoints"] = numPoints.ToString()
                    }
                };

                walls.Add(wallData);
            }

            return walls;
        }

        public async Task<List<StructuralWallData>> ExtractStructuralWallAsync()
        {
            return await Task.Run(ExtractWalls);
        }


        public async Task<ETABSStructuralData> ExtractAllDataAsync()
        {
            var structuralData = new ETABSStructuralData();

            var beamTask = ExtractBeamsAsync();
            var columnTask = ExtractColumnsAsync();
            var slabTask = ExtractSlapAsync();
            var WallTask = ExtractStructuralWallAsync();

            await Task.WhenAll(beamTask, columnTask,slabTask,WallTask);

            structuralData.Beams = beamTask.Result;
            structuralData.Columns = columnTask.Result;
            structuralData.Slabs = slabTask.Result;
            structuralData.StructWalls = WallTask.Result;

            return structuralData;
        }

        public void Dispose()
        {
            ETABSHelper.ReleaseCOMObjects(_model, _etabsObject);
            _model = null;
            _etabsObject = null;
        }

       
    }
}
