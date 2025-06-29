using Microsoft.Win32;

namespace EtabsGeometryExport.Model.HelperClasses
{
    public static class FileHelper
    {
        /// <summary>
        /// Generates a default filename with timestamp
        /// </summary>
        public static string GenerateDefaultFileName() => $"ETABS_Structural_Data_{DateTime.Now:yyyyMMdd_HHmmss}.json";

        /// <summary>
        /// Creates a configured SaveFileDialog
        /// </summary>
        public static SaveFileDialog CreateSaveFileDialog()
        {
            return new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json",
                FileName = GenerateDefaultFileName(),
                Title = "Save ETABS Structural Data"
            };
        }
    }
}
