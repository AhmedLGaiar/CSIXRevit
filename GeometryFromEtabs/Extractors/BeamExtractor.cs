using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ETABSv1;
using ElementsData;
using FromRevit.Utilities;

namespace GeometryFromEtabs.Extractors
{
    public class BeamExtractor : IElementExtractor<BeamGeometryData>
    {
        public string ElementType => "Beam";

        public List<BeamGeometryData> ExtractElements(cSapModel model)
        {
            var beams = new List<BeamGeometryData>();
            var names = HelperData.GetFrameElementNames(model);

            foreach (var name in names)
            {
                if (!HelperData.LabelStartsWith(model, name,"B")) continue;

                var (startPoint, endPoint) = HelperData.GetFrameEndPoints(model, name);
                var (width, height) = HelperData.GetRectangularDimensions(model, name);

                beams.Add(new BeamGeometryData
                {
                    Name = $"{width} x {height}",
                    StartPoint = PointUtilities.FromEtabs(startPoint),
                    EndPoint = PointUtilities.FromEtabs(endPoint),
                    Width = width/1000,
                    Depth = height/1000
                });
            }

            return beams;
        }
    }

      
    
}
