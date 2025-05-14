using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using ToEtabs.Data;
using ToEtabs.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using ToEtabs.Helpers;
using ToEtabs.Models;
using ToEtabs.utilities;
using ToEtabs.data.Beam_Data;
using System.Windows.Controls;
using ETABSv1;
using Newtonsoft.Json;

namespace ToEtabs.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private string jsonPath;
        private List<ColumnData> columns;
        private List<ShearWallData> shearWalls;
        private List<BeamData> beams;
        private readonly cSapModel _sapModel;

        public ObservableCollection<string> DefinedConcreteMatrial { get; private set; }

        [ObservableProperty] private string _selectedConcreteMaterial;

        public MainWindowViewModel(cSapModel SapModel)
        {
            _sapModel = SapModel;

            jsonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Revit_Columns.json");
            columns = ColumnUtilities.LoadColumnData(jsonPath);

            jsonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Revit_StructuralWalls.json");
            shearWalls = ShearWallUtilities.LoadShearWallData(jsonPath);

             jsonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "BeamsData.json");
            BeamsRoot beamsData = BeamUtilities.LoadBeamData(jsonPath);

            beams = beamsData.concreteBeams;


            DefinedConcreteMatrial = new ObservableCollection<string>(MatrialProperties.GetMaterialNames(_sapModel));
        }

        private const double FeetToMeters = 0.3048;

        [RelayCommand]
        private void ExportWallsToEtabs()
        {
            try
            {
                int wallIndex = 1;
                foreach (var wall in shearWalls)
                {
                    // Convert dimensions to meters
                    double thicknessMeters = wall.Thickness * FeetToMeters;
                    double lengthMeters = wall.Length * FeetToMeters;

                    if (!ShearWallHelpers.IsDimensionValid(thicknessMeters) || !ShearWallHelpers.IsDimensionValid(lengthMeters) || !ShearWallHelpers.IsDimensionValid(wall.Height))
                    {
                        System.Diagnostics.Debug.WriteLine($"Skipping wall {wall.Id}: Invalid dimensions (Thickness: {thicknessMeters}, Length: {lengthMeters}, Height: {wall.Height})");
                        continue;
                    }

                    string sectionName = $"SW {thicknessMeters:F3}m";
                    ShearWallUtilities.DefineShearWallSection(_sapModel, sectionName, _selectedConcreteMaterial, thicknessMeters);

                    ShearWallUtilities.DrawShearWallByCoordinates(_sapModel, wall, $"SW{wallIndex}", sectionName, FeetToMeters);

                    wallIndex++;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error exporting walls: {ex.Message}");
                // Optionally: System.Windows.MessageBox.Show($"Error exporting walls: {ex.Message}");
            }
        }

        [RelayCommand]
        private void PushColumnsToEtabs()
        {
            try
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
                    else if (ColumnHelpers.IsApproximately(rotation, 90) || ColumnHelpers.IsApproximately(rotation, 270))
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error exporting columns: {ex.Message}");
            }
        }

        [RelayCommand]
        private void PushBeamsToEtabs()
        {
            try
            {
                _sapModel.SetPresentUnits(eUnits.kN_m_C);

                if (beams == null || beams.Count == 0)
                    return;

                var definedSections = new HashSet<string>();

                foreach (var beam in beams)
                {
                    double widthMM = beam.Section.Width;
                    double depthMM = beam.Section.Depth;
                    string sectionName = $"B_{(int)widthMM}x{(int)depthMM}";

                    if (!definedSections.Contains(sectionName))
                    {
                        string materialName = _selectedConcreteMaterial;

                        int ret = BeamUtilities.DefineBeamSection(_sapModel, sectionName, materialName, depthMM, widthMM);

                        if (ret == 0)
                            BeamUtilities.DefineBeamSection(_sapModel, sectionName, materialName, depthMM, widthMM);

                        definedSections.Add(sectionName);
                    }
                }

                foreach (var beam in beams)
                {
                    double widthMM = beam.Section.Width;
                    double depthMM = beam.Section.Depth;
                    string sectionName = $"B_{(int)widthMM}x{(int)depthMM}";

                    double x1 = beam.StartPoint.X * FeetToMeters;
                    double y1 = beam.StartPoint.Y * FeetToMeters;
                    double z1 = beam.StartPoint.Z * FeetToMeters;

                    double x2 = beam.EndPoint.X * FeetToMeters;
                    double y2 = beam.EndPoint.Y * FeetToMeters;
                    double z2 = beam.EndPoint.Z * FeetToMeters;

                    int ret = BeamUtilities.DrawBeamByCoordinates(_sapModel, x1, y1, z1, x2, y2, z2, beam.Name, sectionName);

                    if (ret == 0)
                        _sapModel.FrameObj.SetSection(beam.Name, sectionName);
                }

                _sapModel.View.RefreshView();
                _sapModel.File.Save();
            }
            catch
            {
               
            }
        }






    }
}
