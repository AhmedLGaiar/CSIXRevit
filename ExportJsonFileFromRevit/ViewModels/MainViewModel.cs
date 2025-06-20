using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElementsData.Geometry;
using ExportJsonFileFromRevit.Services;

namespace ExportJsonFileFromRevit.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IFileService _fileService;
        private readonly IRevitService _revitService;
        private readonly Document _doc;

        [ObservableProperty]
        private string _statusMessage;

        public MainViewModel(IFileService fileService, IRevitService revitService, Document doc)
        {
            _fileService = fileService;
            _revitService = revitService;
            _doc = doc;
            StatusMessage = "Ready to import JSON.";
        }

        [RelayCommand]
        private void ImportBeams()
        {
            try
            {
                string filePath = _fileService.OpenJsonFileDialog();
                if (string.IsNullOrEmpty(filePath))
                {
                    StatusMessage = "No file selected.";
                    return;
                }

                var jsonData = _fileService.ReadJsonFile(filePath);
                _revitService.ProcessBeams(_doc, jsonData.Beams);
                StatusMessage = "Beams processed successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ImportColumns()
        {
            try
            {
                string filePath = _fileService.OpenJsonFileDialog();
                if (string.IsNullOrEmpty(filePath))
                {
                    StatusMessage = "No file selected.";
                    return;
                }

                var jsonData = _fileService.ReadJsonFile(filePath);
                _revitService.ProcessColumns(_doc, jsonData.Columns);
                StatusMessage = "Columns processed successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }
        [RelayCommand]
        private void ImportWalls() // New command for walls
        {
            try
            {
                string filePath = _fileService.OpenJsonFileDialog();
                if (string.IsNullOrEmpty(filePath))
                {
                    StatusMessage = "No file selected.";
                    return;
                }

                var jsonData = _fileService.ReadJsonFile(filePath);
                _revitService.ProcessWalls(_doc, jsonData.StructWalls ?? new List<WallRCData>());
                StatusMessage = "Walls processed successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        
        }
        [RelayCommand]
        private void ImportSlabs()
        {
            try
            {
                string filePath = _fileService.OpenJsonFileDialog();
                if (string.IsNullOrEmpty(filePath))
                {
                    StatusMessage = "No file selected.";
                    return;
                }

                var jsonData = _fileService.ReadJsonFile(filePath);
                _revitService.ProcessSlabs(_doc, jsonData.Slabs ?? new List<SlabData>());
                StatusMessage = "Slabs processed successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }
    }
}