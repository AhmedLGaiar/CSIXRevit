using System;
using System.Collections.Generic;
using System.IO;
using ETABSv1;
using Newtonsoft.Json;
using ToEtabs.Data;
using ToEtabs.Models;

namespace ToEtabs.Utilities
{
    internal class ShearWallUtilities
    {
        public static List<ShearWallData> LoadShearWallData(string jsonPath)
        {
            if (!File.Exists(jsonPath))
                throw new FileNotFoundException($"The file {jsonPath} does not exist.");

            string json = File.ReadAllText(jsonPath);
            List<ShearWallData> shearWallList = JsonConvert.DeserializeObject<List<ShearWallData>>(json);

            return shearWallList;
        }

        public static int DefineShearWallSection(cSapModel sapModel, string name, string materialProp, double thickness)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(materialProp) || thickness <= 0)
            {
                throw new ArgumentException("Invalid input parameters: name, materialProp, or thickness.");
            }

            eShellType shellType = eShellType.ShellThin; // Adjust based on your needs
            int ret = sapModel.PropArea.SetWall(name, eWallPropType.Specified, shellType, materialProp, thickness * 1000);

            if (ret != 0)
            {
                throw new Exception($"Failed to define shear wall section '{name}'. Error code: {ret}");
            }

            return ret;
        }

        public static int DrawShearWallByCoordinates(cSapModel sapModel, double x1, double y1, double z1, double x2, double y2, double z2,
            string wallName, string sectionName)
        {
            string definedName = null;
            int numPoints = 2;
            double[] xCoords = new double[] { x1, x2 };
            double[] yCoords = new double[] { y1, y2 };
            double[] zCoords = new double[] { z1, z2 };

            int ret = sapModel.AreaObj.AddByCoord(numPoints, ref xCoords, ref yCoords, ref zCoords, ref definedName, sectionName, wallName);
            return ret;
        }
    }
}