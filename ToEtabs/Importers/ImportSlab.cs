using ElementsData;
using ETABSv1;

namespace ToEtabs.Importers
{
    public class ImportSlab
    {
        public static void ImportSlabs(List<SlabData> Slabs, cSapModel _sapModel,
            string SelectedConcreteMaterial)
        {
            int slabIndex = 1;

            foreach (SlabData slab in Slabs)
            {
                double thickness = slab.Thickness;
                string slabSectionName = $"S {thickness/ 1000:0.00} m";

                // Define the slab section
                int result = _sapModel.PropArea.SetSlab(
                    slabSectionName,
                    eSlabType.Slab, 
                    eShellType.ShellThin,
                    SelectedConcreteMaterial,
                    thickness
                );

                if (result != 0)
                {
                    Console.WriteLine("Failed to define slab section.");
                }

                int numPoints = slab.OuterBoundary.Count;
                double[] x = new double[numPoints];
                double[] y = new double[numPoints];
                double[] z = new double[numPoints];

                for (int i = 0; i < numPoints; i++)
                {
                    x[i] = slab.OuterBoundary[i].X;
                    y[i] = slab.OuterBoundary[i].Y;
                    z[i] = slab.OuterBoundary[i].Z;
                }

                string name =$"Slab {slabIndex++}";

                int ret = _sapModel.AreaObj.AddByCoord(
                    numPoints,
                    ref x,
                    ref y,
                    ref z,
                    ref name,
                    slabSectionName
                );

                if (ret != 0)
                {
                    Console.WriteLine($"Failed to import slab: {name}");
                }

            }
        }
    }
}