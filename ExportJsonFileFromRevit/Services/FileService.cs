using ExportJsonFileFromRevit.Models;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportJsonFileFromRevit.Services
{
    public class FileService : IFileService
    {
        public string OpenJsonFileDialog()
        {
            OpenFileDialog openFileDialog = new()
            {
                Title = "Select JSON File",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            bool? result = openFileDialog.ShowDialog();
            return result == true ? openFileDialog.FileName : null;
        }

        public JsonStructure ReadJsonFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("JSON file not found.");

            try
            {
                string jsonContent = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<JsonStructure>(jsonContent);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to read JSON file: {ex.Message}");
            }
        }
    }
}
