using System;
using System.IO;
using System.Xml;
using EtabsGempetryExport.Model.HelperClasses;
using Newtonsoft.Json;

namespace EtabsGempetryExport.Model.Service
{
    public class FileService : IFileService
    {
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
                throw new Exception($"Error in SaveWithDialog: {ex.Message}\nStackTrace: {ex.StackTrace}", ex);
            }
        }

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
                throw new Exception($"Error saving file to {filePath}: {ex.Message}\nStackTrace: {ex.StackTrace}", ex);
            }
        }
    }
}