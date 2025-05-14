using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ETABSv1;
using Newtonsoft.Json;
using ToEtabs.data.Beam_Data;

namespace ToEtabs.utilities
{
    internal class BeamUtilities
    {
        public static BeamsRoot LoadBeamData(string jsonPath)
        {
            if (!File.Exists(jsonPath))
                throw new FileNotFoundException($"The file {jsonPath} does not exist.");

            string json = File.ReadAllText(jsonPath);
            BeamsRoot data = JsonConvert.DeserializeObject<BeamsRoot>(json);

            return data;
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
