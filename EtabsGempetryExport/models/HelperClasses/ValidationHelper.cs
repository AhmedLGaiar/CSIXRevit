using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ETABSv1;

namespace EtabsGempetryExport.Model.HelperClasses
{
    public static class ValidationHelper
    {
        /// <summary>
        /// Validates that the model is connected
        /// </summary>
        public static void EnsureModelConnected(cSapModel model)
        {
            if (model == null)
                throw new InvalidOperationException("Not connected to ETABS. Call ConnectToETABS() first.");
        }

        /// <summary>
        /// Validates file path
        /// </summary>
        public static void ValidateFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }
    }
}
