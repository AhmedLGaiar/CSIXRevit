using System.IO;
using ETABSv1;
using Newtonsoft.Json;
using ElementsData.Geometry;

namespace ToEtabs.Utilities
{
    internal class ShearWallUtilities
    {
        public static List<StructuralWallData> LoadShearWallData(string jsonPath)
        {
            if (!File.Exists(jsonPath))
                throw new FileNotFoundException($"The file {jsonPath} does not exist.");

            string json = File.ReadAllText(jsonPath);
            List<StructuralWallData> shearWallList = JsonConvert.DeserializeObject<List<StructuralWallData>>(json);

            return shearWallList;
        }

        public static int DefineShearWallSection(cSapModel sapModel, string name, string materialProp, double thickness)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(materialProp) || thickness <= 0)
            {
                throw new ArgumentException("Invalid input parameters: name, materialProp, or thickness.");
            }

            eShellType shellType = eShellType.ShellThin; // Adjust based on needs
            int ret = sapModel.PropArea.SetWall(name, eWallPropType.Specified, shellType, materialProp, thickness);

            if (ret != 0)
            {
                throw new Exception($"Failed to define shear wall section '{name}'. Error code: {ret}");
            }

            return ret;
        }

        public static int DrawShearWallByCoordinates(cSapModel sapModel, StructuralWallData wall, string wallName, string sectionName)
        {
            if (wall == null || wall.StartPoint == null || wall.EndPoint == null || wall.Height <= 0)
            {
                throw new ArgumentException("Invalid shear wall data: missing points or invalid height.");
            }

            // Convert coordinates and height from feet to meters
            double x1 = wall.StartPoint.X ;
            double y1 = wall.StartPoint.Y ;
            double z1 = wall.StartPoint.Z ;
            double x2 = wall.EndPoint.X;
            double y2 = wall.EndPoint.Y;
            double z2 = wall.EndPoint.Z;
            double heightMeters = wall.Height*1000;

            // Define four points for a rectangular wall (base and top)
            int numPoints = 4;
            double[] xCoords = new double[]
            {
                x1, x2, x2, x1 // Counter-clockwise: bottom-left, bottom-right, top-right, top-left
            };
            double[] yCoords = new double[]
            {
                y1, y2, y2, y1
            };
            double[] zCoords = new double[]
            {
                z1, z2, z2 + heightMeters, z1 + heightMeters
            };

            string definedName = null;
            int ret = sapModel.AreaObj.AddByCoord(numPoints, ref xCoords, ref yCoords, ref zCoords, ref definedName, sectionName, wallName);
            if (ret != 0)
            {
                throw new Exception($"Failed to draw shear wall '{wallName}' with section '{sectionName}'. Error code: {ret}");
            }

            return ret;
        }
    }
}