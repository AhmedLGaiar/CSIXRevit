using ETABSv1;
using System.Windows.Media.Media3D;


namespace EtabsGeometryExport.Model.HelperClasses
{
    public static class FrameHelper
    {
        /// <summary>
        /// Determines if a frame element is a beam based on its label
        /// </summary>
        public static bool IsBeam(string label) => !string.IsNullOrEmpty(label) && label.StartsWith("B", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Determines if a frame element is a column based on its label
        /// </summary>
        public static bool IsColumn(string label) => !string.IsNullOrEmpty(label) && label.StartsWith("C", StringComparison.OrdinalIgnoreCase);


        // ✅ Determine if an area element is a wall
        public static bool IsWall(string label) =>
            !string.IsNullOrEmpty(label) && label.StartsWith("W", StringComparison.OrdinalIgnoreCase);



        /// <summary>
        /// Determines if an area element is a slab based on its label  
        /// </summary>
        public static bool IsSlab(string label) =>
            !string.IsNullOrEmpty(label) && label.StartsWith("F", StringComparison.OrdinalIgnoreCase);




        /// <summary>
        /// Gets the rectangular section properties
        /// </summary>
        public static (double width, double height) GetRectangularSectionProperties(cSapModel model, string sectionName)
        {
            try
            {
                string fileName = "", matProp = "", notes = "", guid = "";
                double t3 = 0, t2 = 0;
                int color = 0;

                int result = model.PropFrame.GetRectangle(sectionName, ref fileName, ref matProp, ref t3, ref t2, ref color, ref notes, ref guid);

                return result == 0 ? (t2, t3) : (0, 0);
            }
            catch (Exception)
            {
                return (0, 0);
            }
        }


        /// <summary>
        /// Gets the slab thickness and type
        /// </summary>
        public static (double thickness, string slabType) GetSlabProperties(cSapModel model, string slabPropName)
        {
            try
            {
                string fileName = "", matProp = "", notes = "", guid = "";
                double thickness = 0;
                int color = 0;
                eShellType eShellType = eShellType.ShellThick;
                eSlabType eSlabType = eSlabType.Drop; 

                int result = model.PropArea.GetSlab(slabPropName, ref eSlabType, ref eShellType,
                                                  ref matProp, ref thickness, ref color, ref notes, ref guid);

                if (result == 0)
                {
                    // الطريقة المُبسطة لتحويل enum إلى string
                    return (thickness, eSlabType.ToString());
                }
                else
                {
                    return (0, "");
                }
            }
            catch (Exception ex)
            {
                // يُفضل إضافة logging للـ exception
                Console.WriteLine($"Error in GetSlabProperties: {ex.Message}");
                return (0, "");
            }
        }



        // ✅ Get wall thickness and material
        public static (double thickness, string material) GetWallProperties(cSapModel model, string wallPropName)
        {
            try
            {
                string matProp = "", notes = "", guid = "";
                double thickness = 0;
                int color = 0;
                eShellType shellType = eShellType.ShellThick;

                eWallPropType WallPropType=eWallPropType.AutoSelectList;

                int result = model.PropArea.GetWall(wallPropName,ref WallPropType, ref shellType, ref matProp, ref thickness,
                                                    ref color, ref notes, ref guid);
                return result == 0 ? (thickness, matProp) : (0, "");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetWallProperties: {ex.Message}");
                return (0, "");
            }
        }



        /// <summary>
        /// Gets the coordinates of a point
        /// </summary>
        public static Point3D GetPointCoordinates(cSapModel model, string pointName)
        {
            if (string.IsNullOrWhiteSpace(pointName))
                throw new ArgumentException("Point name cannot be null or empty.");

            double x = 0, y = 0, z = 0;

            int result = model.PointObj.GetCoordCartesian(pointName, ref x, ref y, ref z);

            if (result != 0)
                throw new Exception($"Failed to get coordinates for point '{pointName}'. ETABS API returned code: {result}");

            return new Point3D { X = x, Y = y, Z = z };
        }



    }
}
