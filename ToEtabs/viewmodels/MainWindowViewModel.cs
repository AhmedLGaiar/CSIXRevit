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

namespace ToEtabs.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        #region CheckBox

        [ObservableProperty] private bool isColumnsChecked;

        [ObservableProperty] private bool isWallsChecked;

        [ObservableProperty] private bool isBeamsChecked;

        [ObservableProperty] private bool isSlabsChecked;

        public bool AnyChecked() =>
            IsColumnsChecked || IsWallsChecked || IsBeamsChecked || IsSlabsChecked;

        [RelayCommand]
        private void SelectAll()
        {
            isColumnsChecked = true;
            isWallsChecked = true;
            isBeamsChecked = true;
            isSlabsChecked = true;
        }

        partial void OnIsColumnsCheckedChanged(bool oldValue, bool newValue) =>
            ExportElementsCommand.NotifyCanExecuteChanged();

        partial void OnIsWallsCheckedChanged(bool oldValue, bool newValue) =>
            ExportElementsCommand.NotifyCanExecuteChanged();

        partial void OnIsBeamsCheckedChanged(bool oldValue, bool newValue) =>
            ExportElementsCommand.NotifyCanExecuteChanged();

        partial void OnIsSlabsCheckedChanged(bool oldValue, bool newValue) =>
            ExportElementsCommand.NotifyCanExecuteChanged();

        #endregion

        private string jsonPath;
        private List<ColumnData> columns;
        private List<StructuralWallData> shearWalls;
        private List<BeamData> beams;
        private readonly cSapModel _sapModel;

        public ObservableCollection<string> DefinedConcreteMatrial { get; private set; }

        [ObservableProperty] private string _selectedConcreteMaterial;

        public MainWindowViewModel(cSapModel SapModel)
        {
            try
            {
                _sapModel = SapModel;

                var openFileDialog = new OpenFileDialog
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

                columns = combinedData.Columns ?? new List<ColumnData>();
                shearWalls = combinedData.Walls ?? new List<StructuralWallData>();
                beams = combinedData.Beams ?? new List<BeamData>();

                DefinedConcreteMatrial = new ObservableCollection<string>(
                    MatrialProperties.GetMaterialNames(_sapModel));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Plugin failed to initialize:\n{ex.Message}", "Plugin Error");
                Environment.Exit(1);
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

            if (IsColumnsChecked)
                ImportColumn.ImportColumns(columns, _sapModel, SelectedConcreteMaterial);

            if (IsWallsChecked)
                ImportWall.ImportWalls(shearWalls, _sapModel, SelectedConcreteMaterial);

            if (IsBeamsChecked)
                ImportBeam.ImportBeams(beams, _sapModel, SelectedConcreteMaterial);

            if (IsSlabsChecked)
            {
                // TODO: Implement ImportSlab if needed
                // ImportSlab.ImportSlabs(slabs, _sapModel, SelectedConcreteMaterial);
            }

            _sapModel.View.RefreshView();
            MessageBox.Show("Import to ETABS completed successfully.", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}