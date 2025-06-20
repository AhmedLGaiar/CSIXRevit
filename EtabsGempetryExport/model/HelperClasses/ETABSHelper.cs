using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ETABSv1;

namespace EtabsGempetryExport.Model.HelperClasses
{
    public static class ETABSHelper
    {
        /// <summary>
        /// Safely gets the active ETABS instance
        /// </summary>
        public static (bool success, cOAPI etabsObject, cSapModel model) GetActiveETABSInstance()
        {
            try
            {
                object obj = Marshal.GetActiveObject("CSI.ETABS.API.ETABSObject");
                var etabsObject = (cOAPI)obj;

                if (etabsObject == null)
                    return (false, null, null);

                var model = etabsObject.SapModel;
                return (true, etabsObject, model);
            }
            catch (Exception)
            {
                return (false, null, null);
            }
        }

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
