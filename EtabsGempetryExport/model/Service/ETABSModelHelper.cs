using ETABSv1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;

namespace EtabsGempetryExport.Model.HelperClasses
{
    public class ETABSModelHelper
    {
        private readonly cSapModel _model;

        public ETABSModelHelper(cSapModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public string[] GetAllFrameNames()
        {
            int numberItems = 0;
            string[] names = Array.Empty<string>();
            _model.FrameObj.GetNameList(ref numberItems, ref names);
            return names;
        }

        public (string label, string story) GetFrameInfo(string frameName)
        {
            string label = "", story = "";
            _model.FrameObj.GetLabelFromName(frameName, ref label, ref story);
            return (label, story);
        }

        public (string startPoint, string endPoint) GetFramePoints(string frameName)
        {
            string startPoint = "", endPoint = "";
            _model.FrameObj.GetPoints(frameName, ref startPoint, ref endPoint);
            return (startPoint, endPoint);
        }

        public string GetFrameSection(string frameName)
        {
            string sectionName = "", autoSelect = "";
            _model.FrameObj.GetSection(frameName, ref sectionName, ref autoSelect);
            return sectionName;
        }

        public (double width, double height) GetSectionDimensions(string sectionName)
        {
            try
            {
                eFramePropType secType = eFramePropType.Rectangular;
                _model.PropFrame.GetTypeOAPI(sectionName, ref secType);

                if (secType == eFramePropType.Rectangular)
                {
                    return FrameHelper.GetRectangularSectionProperties(_model, sectionName);
                }
            }
            catch { }

            return (0, 0);
        }

        public double GetFrameRotation(string frameName)
        {
            double rotation = 0;
            bool advanced = false;
            _model.FrameObj.GetLocalAxes(frameName, ref rotation, ref advanced);
            return rotation;
        }

        public double GetDesignOrientation(string frameName)
        {
            eFrameDesignOrientation orientation = eFrameDesignOrientation.Other;
            _model.FrameObj.GetDesignOrientation(frameName, ref orientation);
            return (double)orientation;
        }

        public (string label, string story) GetPointInfo(string pointName)
        {
            string label = "", story = "";
            _model.PointObj.GetLabelFromName(pointName, ref label, ref story);
            return (label, story);
        }

        public (Point3D StartPoint, Point3D EndPoint, double Length, double Height, double OrientationAngle) CalculateWallGeometry(List<Point3D> points)
        {
            if (points.Count < 2)
                return (new Point3D(), new Point3D(), 0, 0, 0);

            // For a rectangular wall, find the bottom edge (lowest Z values)
            var bottomPoints = points.Where(p => Math.Abs(p.Z - points.Min(pt => pt.Z)) < 0.001).ToList();
            var topPoints = points.Where(p => Math.Abs(p.Z - points.Max(pt => pt.Z)) < 0.001).ToList();

            // Calculate height
            double height = points.Max(p => p.Z) - points.Min(p => p.Z);

            // Get start and end points (use bottom edge)
            Point3D startPoint, endPoint;
            if (bottomPoints.Count >= 2)
            {
                startPoint = bottomPoints[0];
                endPoint = bottomPoints[1];

                // Ensure we get the longer edge if it's a rectangle
                if (bottomPoints.Count >= 3)
                {
                    double dist1 = Distance3D(bottomPoints[0], bottomPoints[1]);
                    double dist2 = Distance3D(bottomPoints[1], bottomPoints[2]);

                    if (dist2 > dist1)
                    {
                        startPoint = bottomPoints[1];
                        endPoint = bottomPoints[2];
                    }
                }
            }
            else
            {
                startPoint = points[0];
                endPoint = points[1];
            }

            // Calculate length
            double length = Distance3D(startPoint, endPoint);

            // Calculate orientation angle (in radians)
            double deltaX = endPoint.X - startPoint.X;
            double deltaY = endPoint.Y - startPoint.Y;
            double orientationAngle = Math.Atan2(deltaY, deltaX);

            return (startPoint, endPoint, length, height, orientationAngle);
        }


        public static double Distance3D(Point3D p1, Point3D p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2) + Math.Pow(p2.Z - p1.Z, 2));
        }
    }
}
