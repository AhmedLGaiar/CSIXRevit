using System;
using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using ToEtabs.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using ToEtabs.Helpers;
using ToEtabs.utilities;
using ElementsData;
using ETABSv1;

namespace ToEtabs.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private string jsonPath;
        private List<ColumnData> columns;
        private List<StructuralWallData> shearWalls;
        private List<BeamData> beams;
        private readonly cSapModel _sapModel;

        public ObservableCollection<string> DefinedConcreteMatrial { get; private set; }

        [ObservableProperty] private string _selectedConcreteMaterial;

        public MainWindowViewModel(cSapModel SapModel)
        {
            _sapModel = SapModel;

            jsonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Revit_Columns.json");
            columns = ColumnUtilities.LoadColumnData(jsonPath);

            jsonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "Revit_StructuralWalls.json");
            shearWalls = ShearWallUtilities.LoadShearWallData(jsonPath);

            jsonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "BeamsData.json");
            beams = BeamUtilities.LoadBeamData(jsonPath);


            DefinedConcreteMatrial = new ObservableCollection<string>(MatrialProperties.GetMaterialNames(_sapModel));
        }

        [RelayCommand]
        private void ExportWallsToEtabs()
        {
            int wallIndex = 1;
            foreach (var wall in shearWalls)
            {
                // Convert dimensions to meters
                double thicknessMeters = wall.Thickness ;
                double lengthMeters = wall.Length ;

                if (!ShearWallHelpers.IsDimensionValid(thicknessMeters) ||
                    !ShearWallHelpers.IsDimensionValid(lengthMeters) ||
                    !ShearWallHelpers.IsDimensionValid(wall.Height))
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Skipping wall {wall.Id}: Invalid dimensions (Thickness: {thicknessMeters}, Length: {lengthMeters}, Height: {wall.Height})");
                    continue;
                }

                string sectionName = $"SW {thicknessMeters:0.00} m";
                ShearWallUtilities.DefineShearWallSection(_sapModel, sectionName, SelectedConcreteMaterial,
                    thicknessMeters*1000);

                ShearWallUtilities.DrawShearWallByCoordinates(_sapModel, wall, $"SW {wallIndex}", sectionName);

                wallIndex++;
            }
            _sapModel.View.RefreshView();

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
                    _selectedConcreteMaterial, depthMeters * 1000, widthMeters * 1000);
                if (done == 0)
                {
                    done = ColumnUtilities.DefineColumnSection(_sapModel, $"C {widthMeters}*{depthMeters} V",
                        _selectedConcreteMaterial, widthMeters * 1000, depthMeters * 1000);
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
                else if (ColumnHelpers.IsApproximately(rotation, 90) ||
                         ColumnHelpers.IsApproximately(rotation, 270))
                {
                    orientation = "H";
                }
                else
                {
                    orientation = "H"; // Default/fallback
                }

                done = ColumnUtilities.DrawColumnByCoordinates(_sapModel,
                    column.BasePoint.X, column.BasePoint.Y, column.BasePoint.Z,
                    column.TopPoint.X, column.TopPoint.Y, column.TopPoint.Z,
                    $"C{ColNum}", $"C {widthMeters}*{depthMeters} {orientation}");

                ColNum++;
            }
            _sapModel.View.RefreshView();
        }

        [RelayCommand]
        private void PushBeamsToEtabs()
        {
            if (beams == null || beams.Count == 0)
                return;

            var definedSections = new HashSet<string>();

            foreach (var beam in beams)
            {
                double widthMM = beam.Width;
                double depthMM = beam.Depth;
                string sectionName = $"B {widthMM}x{depthMM}";

                if (!definedSections.Contains(sectionName))
                {
                    int ret = BeamUtilities.DefineBeamSection(_sapModel, sectionName, SelectedConcreteMaterial, depthMM * 1000,
                        widthMM * 1000);

                    definedSections.Add(sectionName);
                }
            }

            foreach (var beam in beams)
            {
                double widthMM = beam.Width;
                double depthMM = beam.Depth;
                string sectionName = $"B {widthMM}x{depthMM}";

                double x1 = beam.StartPoint.X;
                double y1 = beam.StartPoint.Y;
                double z1 = beam.StartPoint.Z;

                double x2 = beam.EndPoint.X;
                double y2 = beam.EndPoint.Y;
                double z2 = beam.EndPoint.Z;

                int ret = BeamUtilities.DrawBeamByCoordinates(_sapModel, x1, y1, z1, x2, y2, z2, beam.Name,
                    sectionName);

                if (ret == 0)
                    _sapModel.FrameObj.SetSection(beam.Name, sectionName);
            }

            _sapModel.View.RefreshView();
            _sapModel.File.Save();
        }
    }
}