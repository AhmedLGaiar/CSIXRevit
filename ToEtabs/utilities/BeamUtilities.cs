using ElementsData.Geometry;
using ETABSv1;
using Newtonsoft.Json;
using System.IO;

namespace ToEtabs.utilities
{
    internal class BeamUtilities
    {
        public static List<BeamGeometryData> LoadBeamGeometryData(string jsonPath)
        {
            if (!File.Exists(jsonPath))
                throw new FileNotFoundException($"The file {jsonPath} does not exist.");

            string json = File.ReadAllText(jsonPath);
            List<BeamGeometryData> beamsList = JsonConvert.DeserializeObject<List<BeamGeometryData>>(json);

            return beamsList;
        }

        public static int DefineBeamSection(cSapModel SapModel, string Name, string MatrialProp, double depth, double width)
        {
            return SapModel.PropFrame.SetRectangle(Name, MatrialProp, depth, width);
        }

        public static int DrawBeamByCoordinates(cSapModel SapModel, double x1, double y1, double z1, double x2, double y2, double z2,
             string SectionName)
        {
            string DefinedName = null;
            int ret = SapModel.FrameObj.AddByCoord(x1, y1, z1, x2, y2, z2, ref DefinedName, SectionName);
            if (ret == 0)
            {
                double[] Offset1 = [0, 0, 0];
                double[] Offset2 = [0, 0, 0];

                // top center =8
                return ret = SapModel.FrameObj.SetInsertionPoint(DefinedName, 8, false, false,
                    ref Offset1, ref Offset2);
            }
            else
            {
                return 1;
            }
        }
    }

}
