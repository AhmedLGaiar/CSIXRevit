using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FromRevit.Commands;
using FromRevit.ElementsCommand;
using Newtonsoft.Json;
using Microsoft.Win32;

namespace FromRevit.ViewModels
{
    public partial class MainViewViewModel : ObservableObject
    {
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
            ExportDataCommand.NotifyCanExecuteChanged();

        partial void OnIsWallsCheckedChanged(bool oldValue, bool newValue) =>
            ExportDataCommand.NotifyCanExecuteChanged();

        partial void OnIsBeamsCheckedChanged(bool oldValue, bool newValue) =>
            ExportDataCommand.NotifyCanExecuteChanged();

        partial void OnIsSlabsCheckedChanged(bool oldValue, bool newValue) =>
            ExportDataCommand.NotifyCanExecuteChanged();


        [RelayCommand(CanExecute = nameof(AnyChecked))]
        private void ExportData()
        {
            var saveDialog = new SaveFileDialog
            {
                Title = "Export JSON File",
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = "json",
                FileName = "exported_data"
            };

            bool? result = saveDialog.ShowDialog();
            if (result != true || string.IsNullOrWhiteSpace(saveDialog.FileName))
                return;

            // Prepare result object
            var exportData = new Dictionary<string, object>();

            if (IsColumnsChecked)
                exportData["columns"] = Columns.GetColumnData(ExportFromRevit.document);

            if (IsWallsChecked)
                exportData["walls"] = StructuralWall.GetShearWallData(ExportFromRevit.document);

            if (IsBeamsChecked)
                exportData["beams"] = Beams.GetBeamData(ExportFromRevit.document);

            if (IsSlabsChecked)
                exportData["slabs"] = Slabs.GetSlabData(ExportFromRevit.document);

            // Serialize to JSON
            string json = JsonConvert.SerializeObject(exportData, Formatting.Indented);
            File.WriteAllText(saveDialog.FileName, json);

            System.Windows.MessageBox.Show("Export completed successfully!", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}