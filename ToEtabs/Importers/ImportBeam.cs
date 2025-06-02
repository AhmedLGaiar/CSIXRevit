using ElementsData.Geometry;
using ETABSv1;
using ToEtabs.utilities;

namespace ToEtabs.Importers
{
    internal class ImportBeam
    {
        public static void ImportBeams(List<BeamGeometryData> beams, cSapModel _sapModel, string SelectedConcreteMaterial)
        {
            if (beams == null || beams.Count == 0)
                return;

            var definedSections = new HashSet<string>();

            foreach (var beam in beams)
            {
                double widthM = beam.Width;
                double depthM = beam.Depth;
                string sectionName = $"B {widthM:0.00}x{depthM:0.00}";

                if (!definedSections.Contains(sectionName))
                {
                    int ret = BeamUtilities.DefineBeamSection(_sapModel, sectionName, SelectedConcreteMaterial,
                        depthM * 1000,
                        widthM * 1000);

                    definedSections.Add(sectionName);
                }
            }

            foreach (var beam in beams)
            {
                double widthM = beam.Width;
                double depthM = beam.Depth;
                string sectionName = $"B {widthM:0.00}x{depthM:0.00}";

                double x1 = beam.StartPoint.X;
                double y1 = beam.StartPoint.Y;
                double z1 = beam.StartPoint.Z;

                double x2 = beam.EndPoint.X;
                double y2 = beam.EndPoint.Y;
                double z2 = beam.EndPoint.Z;

                int ret = BeamUtilities.DrawBeamByCoordinates(_sapModel, x1, y1, z1
                    , x2, y2, z2, sectionName);
            }
        }
    }
}