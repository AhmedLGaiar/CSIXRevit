using ETABSv1;
using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using ToEtabs.Data;
using ToEtabs.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using ToEtabs.Helpers;

namespace ToEtabs.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private string jsonPath;
        private List<ColumnData> columns;
        private readonly cSapModel _sapModel;

        public ObservableCollection<string> DefinedConcreteMatrial { get; private set; }

        [ObservableProperty] private string _selectedConcreteMaterial;

        public MainWindowViewModel(cSapModel SapModel)
        {
            _sapModel = SapModel;
            jsonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Revit_Columns.json");
            columns = ColumnUtilities.LoadColumnData(jsonPath);

            DefinedConcreteMatrial = new ObservableCollection<string>(MatrialProperties.GetMaterialNames(_sapModel));
        }

        [RelayCommand]
        private void PushColumnsToEtabs()
        {
            int done;
            int ColNum = 1;
            foreach (var column in columns)
            {
                double widthMeters = column.Width;
                double depthMeters = column.depth;

                done = ColumnUtilities.DefineColumnSection(_sapModel, $"C {widthMeters}*{depthMeters} H",
                    SelectedConcreteMaterial
                    , depthMeters * 1000, widthMeters * 1000);
                if (done == 0)
                {
                    done = ColumnUtilities.DefineColumnSection(_sapModel, $"C {widthMeters}*{depthMeters} V",
                        SelectedConcreteMaterial
                        , widthMeters * 1000, depthMeters * 1000);
                }
            }
            foreach (var column in columns)
            {
                double widthMeters = column.Width;
                double depthMeters = column.depth;

                string orientation;
                double rotation = ColumnHelpers.NormalizeAngle(column.Rotation);

                if (ColumnHelpers.IsApproximately(rotation, 0) || ColumnHelpers.IsApproximately(rotation, 180))
                {
                    orientation = "V";
                }
                else if (ColumnHelpers.IsApproximately(rotation, 90) || ColumnHelpers.IsApproximately(rotation, 270))
                {
                    orientation = "H";
                }
                else
                {
                    orientation = "H"; // Default/fallback (or you can throw or handle differently)
                }

                done = ColumnUtilities.DrawColumnByCoordinates(_sapModel,
                    column.BasePoint.X, column.BasePoint.Y, column.BasePoint.Z,
                    column.TopPoint.X, column.TopPoint.Y, column.TopPoint.Z,
                    $"C{ColNum}", $"C {widthMeters}*{depthMeters} {orientation}");

                ColNum++;
            }
        }
    }
}