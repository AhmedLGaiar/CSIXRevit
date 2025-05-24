using ETABSv1;

namespace ToEtabs.Utilities
{
    internal class MatrialProperties
    {
        public static List<string> GetMaterialNames(cSapModel sapModel)
        {
            int numberNames = 0;
            string[] materialNames = null;

            int ret = sapModel.PropMaterial.GetNameList(ref numberNames, ref materialNames, eMatType.Concrete);

            if (ret != 0 || materialNames == null)
            {
                throw new Exception("Failed to retrieve material names from ETABS.");
            }

            return materialNames.ToList();
        }
    }
}
