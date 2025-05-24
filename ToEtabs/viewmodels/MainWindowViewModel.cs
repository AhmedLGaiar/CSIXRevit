using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElementsData;
using ETABSv1;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using ToEtabs.Importers;
using ToEtabs.JsonHandler;
using ToEtabs.Utilities;

namespace ToEtabs.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        #region CheckBox 
        [ObservableProperty]
        private bool isColumnsChecked;
        [ObservableProperty]
        private bool isWallsChecked;
        [ObservableProperty]
        private bool isBeamsChecked;
        [ObservableProperty]
        private bool isSlabsChecked;
        [ObservableProperty]
        private bool isLoadsChecked;
        [ObservableProperty]
        private string selectedFilePath;

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
            ImportElementsCommand.NotifyCanExecuteChanged();

        partial void OnIsWallsCheckedChanged(bool oldValue, bool newValue) =>
            ImportElementsCommand.NotifyCanExecuteChanged();

        partial void OnIsBeamsCheckedChanged(bool oldValue, bool newValue) =>
            ImportElementsCommand.NotifyCanExecuteChanged();

        partial void OnIsSlabsCheckedChanged(bool oldValue, bool newValue) =>
            ImportElementsCommand.NotifyCanExecuteChanged();

        partial void OnIsLoadsCheckedChanged(bool oldValue, bool newValue) =>
            ImportElementsCommand.NotifyCanExecuteChanged();
        #endregion

        private List<ColumnGeometryData> columns;
        private List<StructuralWallData> shearWalls;
        private List<BeamGeometryData> beams;
        private List<SlabData> slabs;
        private readonly cSapModel _sapModel;

        public ObservableCollection<string> DefinedConcreteMatrial { get; private set; }

        [ObservableProperty]
        private string selectedConcreteMaterial;

        public MainWindowViewModel(cSapModel sapModel)
        {
            _sapModel=sapModel;
            DefinedConcreteMatrial = new ObservableCollection<string>(
                               MatrialProperties.GetMaterialNames(_sapModel));
        }

        [RelayCommand(CanExecute = nameof(AnyChecked))]
        private void ImportElements()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select Combined Revit Export JSON File",
                Filter = "JSON Files (*.json)|*.json"
            };

            bool? result = openFileDialog.ShowDialog();
            if (result != true || string.IsNullOrWhiteSpace(openFileDialog.FileName))
            {
                MessageBox.Show("No file selected. App will exit.", "Error");
                Environment.Exit(0);
            }

            string jsonContent = File.ReadAllText(openFileDialog.FileName);
            var combinedData = JsonConvert.DeserializeObject<CombinedElementsData>(jsonContent);

            if (combinedData == null)
            {
                MessageBox.Show("JSON is not in correct format.", "Error");
                Environment.Exit(0);
            }

            columns = combinedData.Columns ?? new List<ColumnGeometryData>();
            shearWalls = combinedData.Walls ?? new List<StructuralWallData>();
            beams = combinedData.Beams ?? new List<BeamGeometryData>();
            slabs = combinedData.Slabs ?? new List<SlabData>();

            if (IsColumnsChecked && columns.Any())
                ImportColumn.ImportColumns(columns, _sapModel, SelectedConcreteMaterial);

            if (IsWallsChecked && shearWalls.Any())
                ImportWall.ImportWalls(shearWalls, _sapModel, SelectedConcreteMaterial);

            if (IsBeamsChecked && beams.Any())
                ImportBeam.ImportBeams(beams, _sapModel, SelectedConcreteMaterial);

            if (IsSlabsChecked && slabs.Any())
                ImportSlab.ImportSlabs(slabs, _sapModel, SelectedConcreteMaterial);

            _sapModel.View.RefreshView();

            MessageBox.Show("Import to ETABS completed successfully.", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}