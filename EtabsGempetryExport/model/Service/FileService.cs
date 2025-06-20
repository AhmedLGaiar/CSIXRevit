using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtabsGempetryExport.Model.HelperClasses;
using System.Xml;
using Newtonsoft.Json;
using System.IO;

namespace EtabsGempetryExport.Model.Service
{
    public class FileService : IFileService
    {
        /// <summary>
        /// Shows save dialog and saves data if user confirms
        /// </summary>
        public bool SaveWithDialog(ETABSStructuralData data, out string filePath)
        {
            filePath = string.Empty;

            try
            {
                var saveFileDialog = FileHelper.CreateSaveFileDialog();

                if (saveFileDialog.ShowDialog() == true)
                {
                    filePath = saveFileDialog.FileName;
                    SaveData(data, filePath);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Saves structural data to specified file path
        /// </summary>
        public void SaveData(ETABSStructuralData data, string filePath)
        {
            try
            {
                ValidationHelper.ValidateFilePath(filePath);

                var json = JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving file to {filePath}: {ex.Message}", ex);
            }
        }
    }

}
