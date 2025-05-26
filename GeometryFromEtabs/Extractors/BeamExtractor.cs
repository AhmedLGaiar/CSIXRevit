using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ETABSv1;
using ElementsData;
using FromRevit.Utilites;

namespace GeometryFromEtabs.Extractors
{
    public class BeamExtractor : IElementExtractor<BeamGeometryData>
    {
        public string ElementType => "Beam";

        public List<BeamGeometryData> ExtractElements(cSapModel model)
        {
            var beams = new List<BeamGeometryData>();

            int numberItems = 0;
            string[] names = new string[0];
            model.FrameObj.GetNameList(ref numberItems, ref names);

            for (int i = 0; i < numberItems; i++)
            {
                string name = names[i];
                string label = "", story = "";
                model.FrameObj.GetLabelFromName(name, ref label, ref story);

                if (!label.StartsWith("B")) continue;

                string start = "", end = "";
                model.FrameObj.GetPoints(name, ref start, ref end);

                double x1 = 0, y1 = 0, z1 = 0;
                double x2 = 0, y2 = 0, z2 = 0;
                model.PointObj.GetCoordCartesian(start, ref x1, ref y1, ref z1);
                model.PointObj.GetCoordCartesian(end, ref x2, ref y2, ref z2);

                string sectionName = "", autoSelect = "";
                model.FrameObj.GetSection(name, ref sectionName, ref autoSelect);

                eFramePropType secType = eFramePropType.Rectangular;
                model.PropFrame.GetTypeOAPI(sectionName, ref secType);

                double width = 0, height = 0;

                if (secType == eFramePropType.Rectangular)
                {
                    string fileName = "", matProp = "", notes = "", guid = "";
                    int color = 0;
                    double t3 = 0, t2 = 0;

                    int ret = model.PropFrame.GetRectangle(sectionName, ref fileName, ref matProp, ref t3, ref t2, ref color, ref notes, ref guid);
                    if (ret == 0)
                    {
                        width = t2;
                        height = t3;
                    }
                }

                beams.Add(new BeamGeometryData
                {
                    Name = $"{width} X {height}",
                    StartPoint = PointUtilites.FromXYZInMilli(x1,y1,z1),
                    EndPoint = PointUtilites.FromXYZInMilli(x2, y2, z2),
                    Width = width,
                    Depth = height
                });
            }

            return beams;
        }
    }

      
    
}
