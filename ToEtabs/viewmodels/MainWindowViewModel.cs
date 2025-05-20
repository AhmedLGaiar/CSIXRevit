using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using ToEtabs.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using ElementsData;
using ETABSv1;
using Newtonsoft.Json;
using ToEtabs.Importers;
using ToEtabs.JsonHandler;
using System.Windows;
using Microsoft.Win32;
using LoadData;

namespace ToEtabs.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        #region CheckBox 
        [ObservableProperty] private bool isColumnsChecked;
        [ObservableProperty] private bool isWallsChecked;
        [ObservableProperty] private bool isBeamsChecked;
        [ObservableProperty] private bool isSlabsChecked;
        [ObservableProperty] private bool isLoadsChecked;
        [ObservableProperty] private string selectedFilePath;

        

        public bool AnyChecked() =>
            IsColumnsChecked || IsWallsChecked || IsBeamsChecked || IsSlabsChecked || IsLoadsChecked;

        [RelayCommand]
        private void SelectAll()
        {
            IsColumnsChecked = true;
            IsWallsChecked = true;
            IsBeamsChecked = true;
            IsSlabsChecked = true;
            IsLoadsChecked = true;
        }

        partial void OnIsColumnsCheckedChanged(bool oldValue, bool newValue) =>
            ExportElementsCommand.NotifyCanExecuteChanged();

        partial void OnIsWallsCheckedChanged(bool oldValue, bool newValue) =>
            ExportElementsCommand.NotifyCanExecuteChanged();

        partial void OnIsBeamsCheckedChanged(bool oldValue, bool newValue) =>
            ExportElementsCommand.NotifyCanExecuteChanged();

        partial void OnIsSlabsCheckedChanged(bool oldValue, bool newValue) =>
            ExportElementsCommand.NotifyCanExecuteChanged();

        partial void OnIsLoadsCheckedChanged(bool oldValue, bool newValue) =>
            ExportElementsCommand.NotifyCanExecuteChanged();
        #endregion

        private string jsonPath;
        private JsonExport loadData;
        private List<ColumnData> columns;
        private List<StructuralWallData> shearWalls;
        private List<BeamData> beams;
        private List<SlabData> slabs;
        private readonly cSapModel _sapModel;

        public ObservableCollection<string> DefinedConcreteMatrial { get; private set; }

        [ObservableProperty] private string selectedConcreteMaterial;

        public MainWindowViewModel(cSapModel sapModel)
        {
            try
            {
                _sapModel = sapModel ?? throw new ArgumentNullException(nameof(sapModel));
                DefinedConcreteMatrial = new ObservableCollection<string>(
                    MatrialProperties.GetMaterialNames(_sapModel));
                columns = new List<ColumnData>();
                shearWalls = new List<StructuralWallData>();
                beams = new List<BeamData>();
                slabs = new List<SlabData>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Plugin failed to initialize:\n{ex.Message}", "Plugin Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }
        }

        [RelayCommand]
        private void BrowseFile()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Select Combined Revit Export JSON File",
                    Filter = "JSON Files (*.json)|*.json"
                };

                if (openFileDialog.ShowDialog() != true || string.IsNullOrWhiteSpace(openFileDialog.FileName))
                {
                    MessageBox.Show("No file selected.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                jsonPath = openFileDialog.FileName;
                SelectedFilePath = Path.GetFileName(jsonPath);

                string jsonContent = File.ReadAllText(jsonPath);
                var combinedData = JsonConvert.DeserializeObject<CombinedElementsData>(jsonContent);

                if (combinedData == null)
                {
                    MessageBox.Show("JSON is not in correct format.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Assign lists with correct types
                columns = combinedData.Columns ?? new List<ColumnData>();
                shearWalls = combinedData.Walls ?? new List<StructuralWallData>();
                beams = combinedData.Beams ?? new List<BeamData>();
                slabs = combinedData.Slabs ?? new List<SlabData>();

                if (IsLoadsChecked)
                {
                    loadData = JsonConvert.DeserializeObject<JsonExport>(jsonContent);
                    if (loadData == null)
                    {
                        MessageBox.Show("Load data in JSON is not in correct format.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        loadData = null;
                        return;
                    }

                    if (!LoadUtilities.ValidateLoadData(loadData, out var errors))
                    {
                        MessageBox.Show($"Load data validation failed:\n{string.Join("\n", errors)}", "Validation Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        loadData = null;
                        return;
                    }

                    LoadUtilities.PrepareLoadData(loadData);
                }

                Logger.Info($"Successfully loaded JSON file: {jsonPath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load file:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                loadData = null;
                columns = new List<ColumnData>();
                shearWalls = new List<StructuralWallData>();
                beams = new List<BeamData>();
                slabs = new List<SlabData>();
            }
        }

        [RelayCommand(CanExecute = nameof(AnyChecked))]
        private void ExportElements()
        {
            if (string.IsNullOrWhiteSpace(SelectedConcreteMaterial))
            {
                MessageBox.Show("Please select a concrete material.", "Missing Material",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(jsonPath))
            {
                MessageBox.Show("Please select a JSON file using Browse.", "Missing File",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (IsColumnsChecked && columns.Any())
                    ImportColumn.ImportColumns(columns, _sapModel, SelectedConcreteMaterial);

                if (IsWallsChecked && shearWalls.Any())
                    ImportWall.ImportWalls(shearWalls, _sapModel, SelectedConcreteMaterial);

                if (IsBeamsChecked && beams.Any())
                    ImportBeam.ImportBeams(beams, _sapModel, SelectedConcreteMaterial);

                if (IsSlabsChecked && slabs.Any())
                    ImportSlab.ImportSlabs(slabs, _sapModel, SelectedConcreteMaterial);

                if (IsLoadsChecked && loadData != null)
                {
                    // Create a local copy to avoid modifying readonly field
                    cSapModel sapModel = _sapModel;
                    ImportLoad.ImportAllLoadData(loadData, ref sapModel);
                    ImportLoad.ApplyElementLoads(loadData.Elements ?? new List<StructuralElement>());
                }

                _sapModel.View.RefreshView();
                MessageBox.Show("Import to ETABS completed successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Import failed:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.Error($"Import failed: {ex.Message}");
            }
        }
    }
}