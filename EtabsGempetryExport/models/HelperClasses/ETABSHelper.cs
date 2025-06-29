using System.Runtime.InteropServices;

namespace EtabsGeometryExport.Model.HelperClasses
{
    public static class ETABSHelper
    {
        /// <summary>
        /// Safely releases COM objects
        /// </summary>
        public static void ReleaseCOMObjects(params object[] comObjects)
        {
            foreach (var comObject in comObjects)
            {
                if (comObject != null)
                {
                    try
                    {
                        Marshal.ReleaseComObject(comObject);
                    }
                    catch (Exception)
                    {
                        // Ignore errors during COM cleanup
                    }
                }
            }
        }
    }
}