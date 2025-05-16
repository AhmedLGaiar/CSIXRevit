using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.Input;
using FromRevit.Commands;
using FromRevit.ElementsCommand;
using Newtonsoft.Json;

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
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select folder to save exported JSON file";
                folderDialog.ShowNewFolderButton = true;

                DialogResult folderResult = folderDialog.ShowDialog();
                if (folderResult != DialogResult.OK || string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
                    return;

                // Ask user for a file name
                string fileName = Microsoft.VisualBasic.Interaction.InputBox(
                    "Enter the file name for the exported JSON (without extension):",
                    "Export File Name",
                    "exported_data");

                if (string.IsNullOrWhiteSpace(fileName))
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
                {
                    // Replace this with your actual slab export
                    // exportData["slabs"] = Slabs.GetSlabData(ExportFromRevit.document);
                }

                string json = JsonConvert.SerializeObject(exportData, Formatting.Indented);
                string fullPath = Path.Combine(folderDialog.SelectedPath, $"{fileName}.json");
                File.WriteAllText(fullPath, json);

                System.Windows.MessageBox.Show("Export completed successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}