using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElementsData.Geometry;
using ExportJsonFileFromRevit.Services;
using System.IO;

namespace ExportJsonFileFromRevit.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IFileService _fileService;
        private readonly IRevitService _revitService;
        private readonly Document _doc;
        // Stores the path of the selected JSON file for reuse
        private string _selectedFilePath;

        [ObservableProperty]
        private string _statusMessage;

        // Property to display the selected file name in the UI
        [ObservableProperty]
        private string _selectedFileName;

        public MainViewModel(IFileService fileService, IRevitService revitService, Document doc)
        {
            _fileService = fileService;
            _revitService = revitService;
            _doc = doc;
            StatusMessage = "Ready to import JSON.";
            SelectedFileName = "No file selected";
        }

        // Command to browse and select a JSON file
        [RelayCommand]
        private void BrowseJson()
        {
            try
            {
                string filePath = _fileService.OpenJsonFileDialog();
                if (string.IsNullOrEmpty(filePath))
                {
                    StatusMessage = "No file selected.";
                    SelectedFileName = "No file selected";
                    _selectedFilePath = null;
                    return;
                }

                // Validate file existence
                if (!File.Exists(filePath))
                {
                    StatusMessage = "Selected file does not exist.";
                    SelectedFileName = "No file selected";
                    _selectedFilePath = null;
                    return;
                }

                _selectedFilePath = filePath;
                SelectedFileName = Path.GetFileName(filePath);
                StatusMessage = "JSON file selected successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error selecting file: {ex.Message}";
                SelectedFileName = "No file selected";
                _selectedFilePath = null;
            }
        }

        [RelayCommand]
        private void ImportBeams()
        {
            try
            {
                // Check if a file has been selected
                if (string.IsNullOrEmpty(_selectedFilePath))
                {
                    StatusMessage = "Please select a JSON file first.";
                    return;
                }

                var jsonData = _fileService.ReadJsonFile(_selectedFilePath);
                // Validate beam data
                if (jsonData?.Beams == null || !jsonData.Beams.Any())
                {
                    StatusMessage = "No beam data found in the selected file.";
                    return;
                }

                _revitService.ProcessBeams(_doc, jsonData.Beams);
                StatusMessage = "Beams processed successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error importing beams: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ImportColumns()
        {
            try
            {
                // Check if a file has been selected
                if (string.IsNullOrEmpty(_selectedFilePath))
                {
                    StatusMessage = "Please select a JSON file first.";
                    return;
                }

                var jsonData = _fileService.ReadJsonFile(_selectedFilePath);
                // Validate column data
                if (jsonData?.Columns == null || !jsonData.Columns.Any())
                {
                    StatusMessage = "No column data found in the selected file.";
                    return;
                }

                _revitService.ProcessColumns(_doc, jsonData.Columns);
                StatusMessage = "Columns processed successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error importing columns: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ImportWalls() // New command for walls
        {
            try
            {
                // Check if a file has been selected
                if (string.IsNullOrEmpty(_selectedFilePath))
                {
                    StatusMessage = "Please select a JSON file first.";
                    return;
                }

                var jsonData = _fileService.ReadJsonFile(_selectedFilePath);
                // Validate wall data
                if (jsonData?.StructWalls == null || !jsonData.StructWalls.Any())
                {
                    StatusMessage = "No wall data found in the selected file.";
                    return;
                }

                _revitService.ProcessWalls(_doc, jsonData.StructWalls);
                StatusMessage = "Walls processed successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error importing walls: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ImportSlabs()
        {
            try
            {
                // Check if a file has been selected
                if (string.IsNullOrEmpty(_selectedFilePath))
                {
                    StatusMessage = "Please select a JSON file first.";
                    return;
                }

                var jsonData = _fileService.ReadJsonFile(_selectedFilePath);
                // Validate slab data
                if (jsonData?.Slabs == null || !jsonData.Slabs.Any())
                {
                    StatusMessage = "No slab data found in the selected file.";
                    return;
                }

                _revitService.ProcessSlabs(_doc, jsonData.Slabs);
                StatusMessage = "Slabs processed successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error importing slabs: {ex.Message}";
            }
        }

        [RelayCommand]
        private void CancelOperation()
        {
            // Placeholder for cancel logic, if needed
            StatusMessage = "Operation pédagogie.";
        }
    }
}