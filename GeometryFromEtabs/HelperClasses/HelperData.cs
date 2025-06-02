using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElementsData.Geometry;
using ETABSv1;

namespace GeometryFromEtabs
{
    public static class HelperData
    {
        public static List<string> GetFrameElementNames(cSapModel model)
        {
            int numberItems = 0;
            string[] names = new string[0];
            model.FrameObj.GetNameList(ref numberItems, ref names);
            return new List<string>(names);
        }

        public static bool LabelStartsWith(cSapModel model, string elementName, string prefix)
        {
            string label = "", story = "";
            model.FrameObj.GetLabelFromName(elementName, ref label, ref story);
            return label.StartsWith(prefix);
        }

        public static (PointData start, PointData end) GetFrameEndPoints(cSapModel model, string elementName)
        {
            string startName = "", endName = "";
            model.FrameObj.GetPoints(elementName, ref startName, ref endName);

            double x1 = 0, y1 = 0, z1 = 0;
            double x2 = 0, y2 = 0, z2 = 0;

            model.PointObj.GetCoordCartesian(startName, ref x1, ref y1, ref z1);
            model.PointObj.GetCoordCartesian(endName, ref x2, ref y2, ref z2);

            return (new PointData { X = x1, Y = y1, Z = z1 }, new PointData { X = x2, Y = y2, Z = z2 });
        }

        public static (double width, double height) GetRectangularDimensions(cSapModel model, string elementName)
        {
            string sectionName = "", autoSelect = "";
            model.FrameObj.GetSection(elementName, ref sectionName, ref autoSelect);

            eFramePropType secType = eFramePropType.Rectangular;
            model.PropFrame.GetTypeOAPI(sectionName, ref secType);

            if (secType == eFramePropType.Rectangular)
            {
                string fileName = "", matProp = "", notes = "", guid = "";
                int color = 0;
                double t3 = 0, t2 = 0;

                int ret = model.PropFrame.GetRectangle(sectionName, ref fileName, ref matProp, ref t3, ref t2, ref color, ref notes, ref guid);
                if (ret == 0)
                {
                    return (t2, t3); // (width, height)
                }
            }

            return (0, 0);
        }
    }
}
