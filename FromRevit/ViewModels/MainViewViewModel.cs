using System.IO;
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
        partial void OnIsColumnsCheckedChanged(bool oldValue, bool newValue) => ExportDataCommand.NotifyCanExecuteChanged();
        partial void OnIsWallsCheckedChanged(bool oldValue, bool newValue) => ExportDataCommand.NotifyCanExecuteChanged();
        partial void OnIsBeamsCheckedChanged(bool oldValue, bool newValue) => ExportDataCommand.NotifyCanExecuteChanged();
        partial void OnIsSlabsCheckedChanged(bool oldValue, bool newValue) => ExportDataCommand.NotifyCanExecuteChanged();


        [RelayCommand(CanExecute = nameof(AnyChecked))]
        private void ExportData()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select folder to save exported JSON files";
                dialog.ShowNewFolderButton = true;

                DialogResult result = dialog.ShowDialog();
                if (result != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SelectedPath))
                    return;

                string folderPath = dialog.SelectedPath;
                if (IsColumnsChecked)
                {
                    var columnList = Columns.GetColumnData(ExportFromRevit.document);
                    string columnJson = JsonConvert.SerializeObject(columnList, Formatting.Indented);
                    File.WriteAllText(Path.Combine(folderPath, "columns.json"), columnJson);
                }

                if (IsWallsChecked)
                {
                    var wallList = StructuralWall.GetShearWallData(ExportFromRevit.document);
                    string wallJson = JsonConvert.SerializeObject(wallList, Formatting.Indented);
                    File.WriteAllText(Path.Combine(folderPath, "walls.json"), wallJson);
                }

                if (IsBeamsChecked)
                {
                    var beamList = Beams.GetBeamData(ExportFromRevit.document);
                    string beamJson = JsonConvert.SerializeObject(beamList, Formatting.Indented);
                    File.WriteAllText(Path.Combine(folderPath, "beams.json"), beamJson);
                }

                if (IsSlabsChecked)
                {
                    //Example slab export placeholder
                    //var slabList = Slabs.GetSlabData(ExportFromRevit.document); // You define this
                    //string slabJson = JsonConvert.SerializeObject(slabList, Formatting.Indented);
                    //File.WriteAllText(Path.Combine(folderPath, "slabs.json"), slabJson);
                }

                System.Windows.MessageBox.Show("Export completed successfully!", "Success",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }
    }
}