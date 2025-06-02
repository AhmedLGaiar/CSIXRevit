using ElementsData.Geometry;
using ETABSv1;
using ToEtabs.Helpers;
using ToEtabs.Utilities;

namespace ToEtabs.Importers
{
    internal class ImportWall
    {
        public static void ImportWalls(List<StructuralWallData> shearWalls, cSapModel _sapModel,
            string SelectedConcreteMaterial)
        {
            int wallIndex = 1;
            foreach (var wall in shearWalls)
            {
                // Convert dimensions to meters
                double thicknessMeters = wall.Thickness;
                double lengthMeters = wall.Length;

                if (!ShearWallHelpers.IsDimensionValid(thicknessMeters) ||
                    !ShearWallHelpers.IsDimensionValid(lengthMeters) ||
                    !ShearWallHelpers.IsDimensionValid(wall.Height))
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Skipping wall {wall.Id}: Invalid dimensions (Thickness: {thicknessMeters}, Length: {lengthMeters}, Height: {wall.Height})");
                    continue;
                }

                string sectionName = $"SW {thicknessMeters:0.00} m";
                ShearWallUtilities.DefineShearWallSection(_sapModel, sectionName, SelectedConcreteMaterial,
                    thicknessMeters * 1000);

                ShearWallUtilities.DrawShearWallByCoordinates(_sapModel, wall, $"SW {wallIndex}", sectionName);

                wallIndex++;
            }
        }
    }
}