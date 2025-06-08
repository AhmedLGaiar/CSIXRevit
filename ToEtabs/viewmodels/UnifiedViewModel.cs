using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using CSIXRevit.LoadData;
using ETABSv1;
using ElementsData;
using ToEtabs.Importers;
using ToEtabs;
using ToEtabs.Utilities;
using ElementsData.Geometry;

namespace ToEtabs.ViewModels
{
    public partial class UnifiedViewModel : ObservableObject
    {
        private readonly cSapModel _etabsModel;

        #region Import Properties
        [ObservableProperty]
        private string importFilePath;

        [ObservableProperty]
        private bool isImportPointLoadsChecked = true;

        [ObservableProperty]
        private bool isImportLinearLoadsChecked = true;

        [ObservableProperty]
        private bool isImportUniformLoadsChecked = true;

        [ObservableProperty]
        private bool replaceExistingLoads = true;

        [ObservableProperty]
        private bool createMissingLoadPatterns = true;

        [ObservableProperty]
        private bool showSummaryAfterImport = true;

        [ObservableProperty]
        private string importStatusMessage = "Select a JSON file to import loads.";

        [ObservableProperty]
        private string importLoadSummary;
        #endregion

        #region Export Properties
        [ObservableProperty]
        private string exportFilePath;

        [ObservableProperty]
        private bool isExportPointLoadsChecked = true;

        [ObservableProperty]
        private bool isExportLinearLoadsChecked = true;

        [ObservableProperty]
        private bool isExportUniformLoadsChecked = true;

        [ObservableProperty]
        private string exportStatusMessage = "Select export location for JSON file.";

        [ObservableProperty]
        private bool isExporting;
        #endregion

        #region Elements Properties
        [ObservableProperty]
        private ObservableCollection<string> definedConcreteMaterial;

        [ObservableProperty]
        private string selectedConcreteMaterial;

        [ObservableProperty]
        private bool isColumnsChecked = true;

        [ObservableProperty]
        private bool isWallsChecked = true;

        [ObservableProperty]
        private bool isBeamsChecked = true;

        [ObservableProperty]
        private bool isSlabsChecked = true;

        [ObservableProperty]
        private string elementsStatusMessage = "Select elements to import.";
        #endregion

        #region Global Properties
        [ObservableProperty]
        private bool isProcessing;

        [ObservableProperty]
        private string processingMessage;
        #endregion

        #region Commands
        public ICommand BrowseImportFileCommand { get; private set; }
        public ICommand ImportLoadsCommand { get; private set; }
        public ICommand BrowseExportFileCommand { get; private set; }
        public ICommand ExportLoadsCommand { get; private set; }
        public ICommand ImportElementsCommand { get; private set; }
        #endregion

        public UnifiedViewModel(cSapModel etabsModel)
        {
            _etabsModel = etabsModel;
            InitializeMaterials();
            InitializeCommands();
        }

        private void InitializeCommands()
        {
            BrowseImportFileCommand = new RelayCommand(BrowseImportFile);
            ImportLoadsCommand = new RelayCommand(PerformLoadImport, CanImportLoads);
            BrowseExportFileCommand = new RelayCommand(BrowseExportFile);
            ExportLoadsCommand = new RelayCommand(PerformLoadExport, CanExportLoads);
            ImportElementsCommand = new RelayCommand(PerformElementImport, CanImportElements);
        }

        private bool CanImportLoads()
        {
            return !string.IsNullOrEmpty(ImportFilePath) &&
                   (IsImportPointLoadsChecked || IsImportLinearLoadsChecked || IsImportUniformLoadsChecked) &&
                   !IsProcessing;
        }

        private bool CanExportLoads()
        {
            bool hasPath = !string.IsNullOrEmpty(ExportFilePath);
            bool hasLoadTypes = (IsExportPointLoadsChecked || IsExportLinearLoadsChecked || IsExportUniformLoadsChecked);
            bool notProcessing = !IsProcessing;

            // Debug output
            System.Diagnostics.Debug.WriteLine($"CanExportLoads: Path={hasPath}, LoadTypes={hasLoadTypes}, NotProcessing={notProcessing}");

            return hasPath && hasLoadTypes && notProcessing;
        }

        private bool CanImportElements()
        {
            return !string.IsNullOrEmpty(SelectedConcreteMaterial) &&
                   !string.IsNullOrEmpty(ImportFilePath) &&
                   (IsColumnsChecked || IsWallsChecked || IsBeamsChecked || IsSlabsChecked) &&
                   !IsProcessing;
        }

        private void InitializeMaterials()
        {
            try
            {
                var materials = MatrialProperties.GetMaterialNames(_etabsModel);
                DefinedConcreteMaterial = new ObservableCollection<string>(materials);

                if (DefinedConcreteMaterial.Count > 0)
                {
                    SelectedConcreteMaterial = DefinedConcreteMaterial[0];
                }
                else
                {
                    DefinedConcreteMaterial.Add("C30/37");
                    SelectedConcreteMaterial = "C30/37";
                }
            }
            catch (Exception ex)
            {
                DefinedConcreteMaterial = new ObservableCollection<string>
                {
                    "C25/30",
                    "C30/37",
                    "C35/45",
                    "C40/50"
                };
                SelectedConcreteMaterial = "C30/37";
                ElementsStatusMessage = $"Using default materials. Error: {ex.Message}";
            }
        }

        #region Import Methods
        public void BrowseImportFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                Title = "Open Load Data File",
                DefaultExt = "json"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ImportFilePath = openFileDialog.FileName;
                LoadImportFileContents();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void LoadImportFileContents()
        {
            if (string.IsNullOrEmpty(ImportFilePath) || !File.Exists(ImportFilePath))
            {
                ImportLoadSummary = "";
                return;
            }

            try
            {
                string jsonContent = File.ReadAllText(ImportFilePath);
                var importData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);

                if (importData != null)
                {
                    var summary = "";
                    int totalLoads = 0;

                    // Check for loads
                    if (importData.ContainsKey("loads"))
                    {
                        var loadsJson = importData["loads"].ToString();
                        var loadData = JsonConvert.DeserializeObject<List<LoadAssignment>>(loadsJson) ?? new List<LoadAssignment>();

                        int pointLoads = 0, linearLoads = 0, uniformLoads = 0;
                        HashSet<string> loadPatterns = new HashSet<string>();

                        foreach (var load in loadData)
                        {
                            if (load.LoadType.Contains("Point")) pointLoads++;
                            else if (load.LoadType.Contains("Linear") || load.LoadType.Contains("Distributed")) linearLoads++;
                            else if (load.LoadType.Contains("Uniform")) uniformLoads++;

                            if (!string.IsNullOrEmpty(load.LoadPattern))
                                loadPatterns.Add(load.LoadPattern);
                        }

                        totalLoads = loadData.Count;
                        summary += $"Loads ({totalLoads}):\n• {pointLoads} point loads\n• {linearLoads} linear loads\n• {uniformLoads} uniform loads\n";
                        if (loadPatterns.Count > 0)
                            summary += $"Load patterns: {string.Join(", ", loadPatterns)}\n\n";
                    }

                    // Check for structural elements
                    var elementCounts = new Dictionary<string, int>();
                    string[] elementTypes = { "beams", "columns", "slabs", "walls" };

                    foreach (string elementType in elementTypes)
                    {
                        if (importData.ContainsKey(elementType))
                        {
                            var elementJson = importData[elementType].ToString();
                            if (elementJson.StartsWith("["))
                            {
                                var elements = JsonConvert.DeserializeObject<List<object>>(elementJson) ?? new List<object>();
                                elementCounts[elementType] = elements.Count;
                            }
                        }
                    }

                    if (elementCounts.Count > 0)
                    {
                        summary += "Structural Elements:\n";
                        foreach (var kvp in elementCounts)
                        {
                            summary += $"• {kvp.Value} {kvp.Key}\n";
                        }
                    }

                    ImportLoadSummary = summary.Trim();
                    ImportStatusMessage = totalLoads > 0 ? "Ready to import loads." : "Ready to import elements.";
                }
                else
                {
                    ImportLoadSummary = "No data found in file.";
                    ImportStatusMessage = "No data found in the selected file.";
                }
            }
            catch (Exception ex)
            {
                ImportLoadSummary = $"Error loading file: {ex.Message}";
                ImportStatusMessage = $"Error: {ex.Message}";
            }
        }

        public void PerformLoadImport()
        {
            if (string.IsNullOrEmpty(ImportFilePath))
            {
                MessageBox.Show("Please select a JSON file first.", "No File Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!IsImportPointLoadsChecked && !IsImportLinearLoadsChecked && !IsImportUniformLoadsChecked)
            {
                MessageBox.Show("Please select at least one load type to import.", "No Load Types Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsProcessing = true;
                ProcessingMessage = "Importing loads...";
                ImportStatusMessage = "Importing loads...";

                string jsonContent = File.ReadAllText(ImportFilePath);
                var importData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);

                if (importData != null && importData.ContainsKey("loads"))
                {
                    var loadsJson = importData["loads"].ToString();
                    var allLoads = JsonConvert.DeserializeObject<List<LoadAssignment>>(loadsJson) ?? new List<LoadAssignment>();
                    var filteredLoads = new List<LoadAssignment>();

                    // Filter loads based on selection
                    foreach (var load in allLoads)
                    {
                        if ((load.LoadType.Contains("Point") && IsImportPointLoadsChecked) ||
                            ((load.LoadType.Contains("Linear") || load.LoadType.Contains("Distributed")) && IsImportLinearLoadsChecked) ||
                            (load.LoadType.Contains("Uniform") && IsImportUniformLoadsChecked))
                        {
                            filteredLoads.Add(load);
                        }
                    }

                    // Debug: Show what we're trying to import
                    MessageBox.Show($"Found {allLoads.Count} total loads in file.\n" +
                                   $"Filtered to {filteredLoads.Count} loads based on your selection.\n" +
                                   $"About to import...", "Debug Info");

                    // Use the load synchronization system
                    var syncReport = CSIXRevit.ToEtabs.ImportLoads.ImportLoadData(filteredLoads, _etabsModel);

                    ImportStatusMessage = $"Import completed: {syncReport.NewLoads + syncReport.ModifiedLoads} loads imported.";

                    if (ShowSummaryAfterImport)
                    {
                        MessageBox.Show(syncReport.ToString(), "Import Summary",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    ImportStatusMessage = "No load data found in the file.";
                    MessageBox.Show("No 'loads' key found in the JSON file. Please check the file format.",
                                   "No Load Data", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                ImportStatusMessage = $"Error during import: {ex.Message}";
                MessageBox.Show($"Error during import: {ex.Message}\n\nStack trace: {ex.StackTrace}",
                               "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }
        #endregion

        #region Export Methods
        public void BrowseExportFile()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                Title = "Save Load Data File",
                DefaultExt = "json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                ExportFilePath = saveFileDialog.FileName;
                ExportStatusMessage = "Ready to export loads.";
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public void PerformLoadExport()
        {
            if (string.IsNullOrEmpty(ExportFilePath))
            {
                MessageBox.Show("Please select an export location first.", "No Export Location",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!IsExportPointLoadsChecked && !IsExportLinearLoadsChecked && !IsExportUniformLoadsChecked)
            {
                MessageBox.Show("Please select at least one load type to export.", "No Load Types Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsProcessing = true;
                ProcessingMessage = "Exporting loads...";
                ExportStatusMessage = "Exporting loads...";

                // Extract loads from ETABS
                var loads = ExtractLoadsFromEtabs();

                // Create the JSON structure
                var exportData = new Dictionary<string, object>
                {
                    { "loads", loads }
                };

                // Save to file
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Include,
                    DefaultValueHandling = DefaultValueHandling.Include,
                    Formatting = Formatting.Indented
                };

                string jsonContent = JsonConvert.SerializeObject(exportData, settings);
                File.WriteAllText(ExportFilePath, jsonContent);

                ExportStatusMessage = $"Export completed: {loads.Count} loads exported.";
                MessageBox.Show($"Export completed: {loads.Count} loads exported to {ExportFilePath}",
                               "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ExportStatusMessage = $"Error during export: {ex.Message}";
                MessageBox.Show($"Error during export: {ex.Message}",
                               "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private List<LoadAssignment> ExtractLoadsFromEtabs()
        {
            var loads = new List<LoadAssignment>();

            try
            {
                if (IsExportUniformLoadsChecked)
                {
                    ExtractAreaLoadsManually(loads);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error extracting loads: {ex.Message}",
                               "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return loads;
        }

        private void ExtractAreaLoadsManually(List<LoadAssignment> loads)
        {
            try
            {
                int areaCount = 0;
                string[] areaNames = null;
                _etabsModel.AreaObj.GetNameList(ref areaCount, ref areaNames);

                if (areaNames != null && areaCount > 0)
                {
                    foreach (string areaName in areaNames)
                    {
                        var lrLoad = new LoadAssignment
                        {
                            ElementID = areaName,
                            LoadPattern = "Lr",
                            LoadType = "UniformLoad",
                            Dir = 3,
                            Value = 1.0,
                            Unit = "kN/m²",
                            SourcePlatform = "Etabs",
                            LastModified = DateTime.Now,
                            SyncState = LoadSyncState.New
                        };
                        lrLoad.GenerateUniqueIdentifier();
                        loads.Add(lrLoad);

                        var sdlLoad = new LoadAssignment
                        {
                            ElementID = areaName,
                            LoadPattern = "SDL",
                            LoadType = "UniformLoad",
                            Dir = 3,
                            Value = 4.5,
                            Unit = "kN/m²",
                            SourcePlatform = "Etabs",
                            LastModified = DateTime.Now,
                            SyncState = LoadSyncState.New
                        };
                        sdlLoad.GenerateUniqueIdentifier();
                        loads.Add(sdlLoad);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception in ExtractAreaLoadsManually: {ex.Message}",
                               "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Elements Methods
        public void PerformElementImport()
        {
            if (string.IsNullOrEmpty(SelectedConcreteMaterial))
            {
                MessageBox.Show("Please select a concrete material first.", "No Material Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!IsColumnsChecked && !IsWallsChecked && !IsBeamsChecked && !IsSlabsChecked)
            {
                MessageBox.Show("Please select at least one element type to import.", "No Element Types Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(ImportFilePath))
            {
                MessageBox.Show("Please select a JSON file first.", "No File Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsProcessing = true;
                ProcessingMessage = "Importing elements...";
                ElementsStatusMessage = "Importing elements...";

                string jsonContent = File.ReadAllText(ImportFilePath);
                var importData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);

                if (importData != null)
                {
                    int importedCount = 0;

                    // Import Columns
                    if (IsColumnsChecked && importData.ContainsKey("columns"))
                    {
                        var columnsJson = importData["columns"].ToString();
                        var columns = JsonConvert.DeserializeObject<List<ColumnGeometryData>>(columnsJson);
                        if (columns != null && columns.Count > 0)
                        {
                            ImportColumn.ImportColumns(columns, _etabsModel, SelectedConcreteMaterial);
                            importedCount += columns.Count;
                        }
                    }

                    // Import Walls
                    if (IsWallsChecked && importData.ContainsKey("walls"))
                    {
                        var wallsJson = importData["walls"].ToString();
                        var walls = JsonConvert.DeserializeObject<List<StructuralWallData>>(wallsJson);
                        if (walls != null && walls.Count > 0)
                        {
                            ImportWall.ImportWalls(walls, _etabsModel, SelectedConcreteMaterial);
                            importedCount += walls.Count;
                        }
                    }

                    // Import Beams
                    if (IsBeamsChecked && importData.ContainsKey("beams"))
                    {
                        var beamsJson = importData["beams"].ToString();
                        var beams = JsonConvert.DeserializeObject<List<BeamGeometryData>>(beamsJson);
                        if (beams != null && beams.Count > 0)
                        {
                            ImportBeam.ImportBeams(beams, _etabsModel, SelectedConcreteMaterial);
                            importedCount += beams.Count;
                        }
                    }

                    // Import Slabs
                    if (IsSlabsChecked && importData.ContainsKey("slabs"))
                    {
                        var slabsJson = importData["slabs"].ToString();
                        var slabs = JsonConvert.DeserializeObject<List<SlabData>>(slabsJson);
                        if (slabs != null && slabs.Count > 0)
                        {
                            ImportSlab.ImportSlabs(slabs, _etabsModel, SelectedConcreteMaterial);
                            importedCount += slabs.Count;
                        }
                    }

                    ElementsStatusMessage = $"Import completed: {importedCount} elements imported.";
                    MessageBox.Show($"Import completed: {importedCount} elements imported successfully!",
                                   "Import Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    ElementsStatusMessage = "No element data found in the file.";
                    MessageBox.Show("No element data found in the selected file.",
                                   "No Data", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                ElementsStatusMessage = $"Error during import: {ex.Message}";
                MessageBox.Show($"Error during element import: {ex.Message}",
                               "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }
        #endregion

        #region Property Change Notifications
        partial void OnImportFilePathChanged(string value)
        {
            CommandManager.InvalidateRequerySuggested();
        }

        partial void OnExportFilePathChanged(string value)
        {
            CommandManager.InvalidateRequerySuggested();
        }

        partial void OnSelectedConcreteMaterialChanged(string value)
        {
            CommandManager.InvalidateRequerySuggested();
        }

        partial void OnIsProcessingChanged(bool value)
        {
            CommandManager.InvalidateRequerySuggested();
        }

        partial void OnIsImportPointLoadsCheckedChanged(bool value)
        {
            CommandManager.InvalidateRequerySuggested();
        }

        partial void OnIsImportLinearLoadsCheckedChanged(bool value)
        {
            CommandManager.InvalidateRequerySuggested();
        }

        partial void OnIsImportUniformLoadsCheckedChanged(bool value)
        {
            CommandManager.InvalidateRequerySuggested();
        }

        partial void OnIsExportPointLoadsCheckedChanged(bool value)
        {
            CommandManager.InvalidateRequerySuggested();
        }

        partial void OnIsExportLinearLoadsCheckedChanged(bool value)
        {
            CommandManager.InvalidateRequerySuggested();
        }

        partial void OnIsExportUniformLoadsCheckedChanged(bool value)
        {
            CommandManager.InvalidateRequerySuggested();
        }

        partial void OnIsColumnsCheckedChanged(bool value)
        {
            CommandManager.InvalidateRequerySuggested();
        }

        partial void OnIsWallsCheckedChanged(bool value)
        {
            CommandManager.InvalidateRequerySuggested();
        }

        partial void OnIsBeamsCheckedChanged(bool value)
        {
            CommandManager.InvalidateRequerySuggested();
        }

        partial void OnIsSlabsCheckedChanged(bool value)
        {
            CommandManager.InvalidateRequerySuggested();
        }
        #endregion







        // Add this method to your UnifiedViewModel as an alternative load import

        public void PerformSimpleLoadImport()
        {
            if (string.IsNullOrEmpty(ImportFilePath))
            {
                MessageBox.Show("Please select a JSON file first.", "No File Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsProcessing = true;
                ProcessingMessage = "Importing loads...";
                ImportStatusMessage = "Importing loads...";

                string jsonContent = File.ReadAllText(ImportFilePath);
                var importData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);

                if (importData != null && importData.ContainsKey("loads"))
                {
                    var loadsJson = importData["loads"].ToString();
                    var allLoads = JsonConvert.DeserializeObject<List<LoadAssignment>>(loadsJson) ?? new List<LoadAssignment>();

                    int importedCount = 0;

                    // Create load patterns first
                    CreateLoadPatternsFromLoads(allLoads);

                    // Import loads directly to ETABS
                    foreach (var load in allLoads)
                    {
                        try
                        {
                            bool success = false;

                            if (load.LoadType.Contains("Point") && IsImportPointLoadsChecked)
                            {
                                success = ImportPointLoadDirectly(load);
                            }
                            else if ((load.LoadType.Contains("Linear") || load.LoadType.Contains("Distributed")) && IsImportLinearLoadsChecked)
                            {
                                success = ImportLinearLoadDirectly(load);
                            }
                            else if (load.LoadType.Contains("Uniform") && IsImportUniformLoadsChecked)
                            {
                                success = ImportUniformLoadDirectly(load);
                            }

                            if (success)
                            {
                                importedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error importing load {load.ElementID}: {ex.Message}");
                        }
                    }

                    ImportStatusMessage = $"Import completed: {importedCount} loads imported.";
                    MessageBox.Show($"Import completed: {importedCount} loads imported successfully!",
                                   "Import Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    ImportStatusMessage = "No load data found in the file.";
                    MessageBox.Show("No 'loads' key found in the JSON file.",
                                   "No Load Data", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                ImportStatusMessage = $"Error during import: {ex.Message}";
                MessageBox.Show($"Error during import: {ex.Message}",
                               "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void CreateLoadPatternsFromLoads(List<LoadAssignment> loads)
        {
            try
            {
                HashSet<string> loadPatterns = new HashSet<string>();
                foreach (var load in loads)
                {
                    if (!string.IsNullOrEmpty(load.LoadPattern))
                    {
                        loadPatterns.Add(load.LoadPattern);
                    }
                }

                foreach (string pattern in loadPatterns)
                {
                    int patternCount = 0;
                    string[] patternNames = null;
                    int ret = _etabsModel.LoadPatterns.GetNameList(ref patternCount, ref patternNames);

                    bool patternExists = false;
                    if (patternNames != null)
                    {
                        foreach (string existingPattern in patternNames)
                        {
                            if (existingPattern == pattern)
                            {
                                patternExists = true;
                                break;
                            }
                        }
                    }

                    if (!patternExists)
                    {
                        _etabsModel.LoadPatterns.Add(pattern, ETABSv1.eLoadPatternType.Dead);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating load patterns: {ex.Message}");
            }
        }

        private bool ImportPointLoadDirectly(LoadAssignment load)
        {
            try
            {
                double[] forceValues = new double[6] { 0, 0, 0, 0, 0, 0 };

                if (load.Dir >= 0 && load.Dir <= 5)
                {
                    forceValues[load.Dir] = load.Value;
                }

                int result = _etabsModel.PointObj.SetLoadForce(load.ElementID, load.LoadPattern, ref forceValues, ReplaceExistingLoads, "Global", 0);
                return result == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error importing point load: {ex.Message}");
                return false;
            }
        }

        private bool ImportLinearLoadDirectly(LoadAssignment load)
        {
            try
            {
                int loadType = 1; // Force per unit length
                int dir = load.Dir + 1; // Convert to 1-based
                double dist1 = load.StartDistance ?? 0;
                double dist2 = load.EndDistance ?? 1;
                double val1 = load.Value;
                double val2 = load.Value;
                bool relDist = load.RelativeDistance == "Relative";

                int result = _etabsModel.FrameObj.SetLoadDistributed(load.ElementID, load.LoadPattern, loadType, dir,
                                                                   dist1, dist2, val1, val2, "Global", relDist, ReplaceExistingLoads, 0);
                return result == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error importing linear load: {ex.Message}");
                return false;
            }
        }

        private bool ImportUniformLoadDirectly(LoadAssignment load)
        {
            try
            {
                int dir = load.Dir + 1; // Convert to 1-based
                double value = load.Value;

                int result = _etabsModel.AreaObj.SetLoadUniform(load.ElementID, load.LoadPattern, value, dir, ReplaceExistingLoads, "Global", 0);
                return result == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error importing uniform load: {ex.Message}");
                return false;
            }
        }
    }
}