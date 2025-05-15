using System.IO;
using ElementsData;
using ETABSv1;
using Newtonsoft.Json;

namespace ToEtabs.utilities
{
    internal class BeamUtilities
    {
        public static List<BeamData> LoadBeamData(string jsonPath)
        {
            if (!File.Exists(jsonPath))
                throw new FileNotFoundException($"The file {jsonPath} does not exist.");

            string json = File.ReadAllText(jsonPath);
            List<BeamData> beamsList = JsonConvert.DeserializeObject<List<BeamData>>(json);

            return beamsList;
        }

        public static int DefineBeamSection(cSapModel SapModel, string Name, string MatrialProp, double depth, double width)
        {
            return SapModel.PropFrame.SetRectangle(Name, MatrialProp, depth, width);
        }

        public static int DrawBeamByCoordinates(cSapModel SapModel, double x1, double y1, double z1, double x2, double y2, double z2,
            string BeamName, string SectionName)
        {
            string DefinedName = null;
            return SapModel.FrameObj.AddByCoord(x1, y1, z1, x2, y2, z2, ref DefinedName, SectionName, BeamName);
        }
    }

}
