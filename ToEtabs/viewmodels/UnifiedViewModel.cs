using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
using loadData; // Added for SyncReport

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
                            if (load.LoadType.Contains("Point") || load.LoadType.Contains("MomentLoad")) pointLoads++;
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
                        if (((load.LoadType.Contains("Point") || load.LoadType.Contains("MomentLoad")) && IsImportPointLoadsChecked) ||
                            ((load.LoadType.Contains("Linear") || load.LoadType.Contains("Distributed")) && IsImportLinearLoadsChecked) ||
                            (load.LoadType.Contains("Uniform") && IsImportUniformLoadsChecked))
                        {
                            filteredLoads.Add(load);
                        }
                    }

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
                MessageBox.Show($"Error during import: {ex.Message}",
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
                DefaultExt = "json",
                FileName = $"ETABS_Loads_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                ExportFilePath = saveFileDialog.FileName;
                ExportStatusMessage = "Ready to export loads in ETABS format.";
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
                ProcessingMessage = "Extracting loads from ETABS...";
                ExportStatusMessage = "Extracting loads from ETABS...";

                // Extract loads directly here with the fixed method
                var allLoads = ExtractLoadsFromEtabs();

                // Filter loads based on user selection
                var filteredLoads = new List<LoadAssignment>();

                foreach (var load in allLoads)
                {
                    bool shouldInclude = false;

                    if (IsExportPointLoadsChecked && (load.LoadType.Contains("PointLoad") || load.LoadType.Contains("MomentLoad")))
                    {
                        shouldInclude = true;
                    }
                    else if (IsExportLinearLoadsChecked && (load.LoadType.Contains("LinearLoad") || load.LoadType.Contains("LinearMomentLoad")))
                    {
                        shouldInclude = true;
                    }
                    else if (IsExportUniformLoadsChecked && (load.LoadType.Contains("UniformLoad") || load.LoadType.Contains("UniformMomentLoad")))
                    {
                        shouldInclude = true;
                    }

                    if (shouldInclude)
                    {
                        filteredLoads.Add(load);
                    }
                }

                // Create export structure
                var exportData = new Dictionary<string, object>
                {
                    { "loads", filteredLoads },
                    { "export_info", new {
                        exported_date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        model_units = "kN-m",
                        export_units = "kN, kN/m, kN/m², kN-m, kN-m/m, kN-m/m²", // Updated units
                        format_version = "1.2", // Increment version
                        total_loads_in_model = allLoads.Count,
                        total_loads_exported = filteredLoads.Count,
                        loads_by_type = filteredLoads.GroupBy(l => l.LoadType).ToDictionary(g => g.Key, g => g.Count()), // Corrected l.Key to l.LoadType
                        loads_by_pattern = filteredLoads.GroupBy(l => l.LoadPattern).ToDictionary(g => g.Key, g => g.Count()),
                        export_filters = new {
                            point_loads = IsExportPointLoadsChecked,
                            linear_loads = IsExportLinearLoadsChecked,
                            uniform_loads = IsExportUniformLoadsChecked
                        },
                        exported_by = "Revit-ETABS Integration v1.2", // Update version
                        export_timestamp = DateTimeOffset.Now.ToUnixTimeSeconds()
                    }}
                };

                // Save to file
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Include,
                    Formatting = Formatting.Indented,
                    DateFormatString = "yyyy-MM-dd HH:mm:ss"
                };

                string jsonContent = JsonConvert.SerializeObject(exportData, settings);
                File.WriteAllText(ExportFilePath, jsonContent);

                ExportStatusMessage = $"Export completed: {filteredLoads.Count} loads exported.";

                // Show summary
                var loadsByType = filteredLoads.GroupBy(l => l.LoadType).ToDictionary(g => g.Key, g => g.Count()); // Corrected l.Key to l.LoadType
                var loadsByPattern = filteredLoads.GroupBy(l => l.LoadPattern).ToDictionary(g => g.Key, g => g.Count());

                string summaryMessage = $"Export completed successfully!\n\n" +
                                      $"File: {Path.GetFileName(ExportFilePath)}\n" +
                                      $"Total loads found: {allLoads.Count}\n" +
                                      $"Loads exported after filtering: {filteredLoads.Count}\n" +
                                      $"Format: ETABS compatible with proper formatting\n\n";

                if (loadsByType.Count > 0)
                {
                    summaryMessage += "Load types exported:\n";
                    foreach (var kvp in loadsByType)
                    {
                        summaryMessage += $"• {kvp.Key}: {kvp.Value}\n";
                    }
                }

                if (loadsByPattern.Count > 0)
                {
                    summaryMessage += "\nLoad patterns:\n";
                    foreach (var kvp in loadsByPattern)
                    {
                        summaryMessage += $"• {kvp.Key}: {kvp.Value}\n";
                    }
                }

                if (filteredLoads.Count == 0)
                {
                    summaryMessage += "\nNote: No loads were found that match your export criteria.\n" +
                                    "Try checking different load types or verify loads exist in ETABS.";
                }

                MessageBox.Show(summaryMessage, "Export Successful",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ExportStatusMessage = $"Error during export: {ex.Message}";
                MessageBox.Show($"Error during export: {ex.Message}\n\nDetails: {ex.StackTrace}",
                               "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // Simplified and reliable load extraction method
        private List<LoadAssignment> ExtractLoadsFromEtabs()
        {
            var loads = new List<LoadAssignment>();

            try
            {
                // Extract Area (Slab/Wall) Uniform Loads
                ExtractAreaUniformLoads(loads);

                // Extract Frame Distributed (Linear) Loads
                ExtractFrameDistributedLoads(loads);

                // Extract Frame Point Loads (Forces and Moments)
                ExtractFramePointLoads(loads);

                System.Diagnostics.Debug.WriteLine($"Successfully extracted {loads.Count} loads from ETABS");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting loads from ETABS: {ex.Message}");
            }

            return loads;
        }

        private void ExtractAreaUniformLoads(List<LoadAssignment> loads)
        {
            try
            {
                int areaCount = 0;
                string[] areaNames = null;
                _etabsModel.AreaObj.GetNameList(ref areaCount, ref areaNames);

                if (areaNames == null) return;

                foreach (string areaName in areaNames)
                {
                    try
                    {
                        int numberItems = 0;
                        string[] loadPatterns = null;
                        string[] loadCases = null;
                        string[] csys = null;
                        int[] dir = null;
                        double[] values = null;

                        int ret = _etabsModel.AreaObj.GetLoadUniform(areaName, ref numberItems,
                            ref loadPatterns, ref loadCases, ref csys, ref dir, ref values);

                        if (ret == 0 && numberItems > 0)
                        {
                            for (int i = 0; i < numberItems; i++)
                            {
                                string loadType;
                                string unit;
                                int mappedDir;
                                string loadDirectionDescription;

                                // Determine load type, unit, and mapped direction based on ETABS API 'dir' value
                                // ETABS API 'dir' values:
                                // 1,2,3: Local 1,2,3 or Global X,Y,Z (depending on CSys)
                                // 4,5,6: Global X,Y,Z (non-projected)
                                // 7,8,9: Global X,Y,Z (projected)
                                // 10,11,12: Moment loads (Mx, My, Mz)
                                if (dir[i] >= 1 && dir[i] <= 3) // Local 1,2,3 or Global X,Y,Z (depending on CSys)
                                {
                                    loadType = "UniformLoad";
                                    unit = "kN/m²";
                                    mappedDir = dir[i] - 1; // Map 1,2,3 to 0,1,2
                                    loadDirectionDescription = $"{csys[i]} {(char)('X' + mappedDir)}";
                                }
                                else if (dir[i] >= 4 && dir[i] <= 6) // Global X,Y,Z (non-projected)
                                {
                                    loadType = "UniformLoad";
                                    unit = "kN/m²";
                                    mappedDir = dir[i] - 4; // Map 4,5,6 to 0,1,2
                                    loadDirectionDescription = $"Global {(char)('X' + mappedDir)}";
                                }
                                else if (dir[i] >= 7 && dir[i] <= 9) // Global X,Y,Z (projected)
                                {
                                    loadType = "UniformLoad"; // Could be "UniformProjectedLoad" if you want more detail
                                    unit = "kN/m²";
                                    mappedDir = dir[i] - 7; // Map 7,8,9 to 0,1,2
                                    loadDirectionDescription = $"Global {(char)('X' + mappedDir)} (Projected)";
                                }
                                else if (dir[i] >= 10 && dir[i] <= 12) // Moment loads (Mx, My, Mz)
                                {
                                    loadType = "UniformMomentLoad";
                                    unit = "kN-m/m²";
                                    mappedDir = dir[i] - 10; // Map 10,11,12 to 0,1,2
                                    loadDirectionDescription = $"Global M{(char)('X' + mappedDir)}";
                                }
                                else
                                {
                                    // Fallback for unexpected dir values
                                    loadType = "UniformLoad";
                                    unit = "kN/m²";
                                    mappedDir = 2; // Default to Z-direction
                                    loadDirectionDescription = "Unknown Direction (Default Z)";
                                }

                                var load = new LoadAssignment
                                {
                                    ElementID = areaName,
                                    LoadPattern = loadPatterns[i],
                                    LoadType = loadType,
                                    Value = values[i], // Keep the sign
                                    Unit = unit,
                                    DisplayUnits = unit,
                                    Dir = mappedDir,
                                    LoadCase = loadCases[i] ?? "Gravity",
                                    FormattedDescription = $"{values[i]:F1} {unit} ({loadCases[i] ?? "Gravity"}) {loadDirectionDescription}",
                                    SourcePlatform = "Etabs",
                                    LastModified = DateTime.Now,
                                    SyncState = LoadSyncState.New
                                };

                                load.GenerateUniqueIdentifier();
                                loads.Add(load);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing area {areaName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting area uniform loads: {ex.Message}");
            }
        }


        private void ExtractFrameDistributedLoads(List<LoadAssignment> loads)
        {
            try
            {
                int frameCount = 0;
                string[] frameNames = null;
                _etabsModel.FrameObj.GetNameList(ref frameCount, ref frameNames);

                if (frameNames == null) return;

                foreach (string frameName in frameNames)
                {
                    try
                    {
                        int numberItems = 0;
                        string[] loadPatterns = null;
                        string[] loadCases = null;
                        int[] myType = null; // 1=Force, 2=Moment
                        string[] csys = null;
                        int[] dir = null; // 1,2,3 for Fx,Fy,Fz; 4,5,6 for Mx,My,Mz, 7,8,9 for Projected, etc.
                        double[] dist1 = null;
                        double[] dist2 = null;
                        double[] val1 = null;
                        double[] val2 = null;
                        double[] val3 = null; // This will now hold the START load value
                        double[] val4 = null; // This will now hold the END load value

                        int ret = _etabsModel.FrameObj.GetLoadDistributed(frameName, ref numberItems,
                            ref loadPatterns, ref loadCases, ref myType, ref csys, ref dir,
                            ref dist1, ref dist2, ref val1, ref val2, ref val3, ref val4, eItemType.Objects);

                        if (ret == 0 && numberItems > 0)
                        {
                            for (int i = 0; i < numberItems; i++)
                            {
                                string loadType;
                                string unit;
                                int mappedDir;
                                string loadDirectionDescription;

                                if (myType[i] == 1) // Force load
                                {
                                    loadType = "LinearLoad";
                                    unit = "kN/m";
                                }
                                else // Moment load (myType[i] == 2)
                                {
                                    loadType = "LinearMomentLoad";
                                    unit = "kN-m/m";
                                }

                                // Determine mapped direction based on ETABS API 'dir' value
                                if (dir[i] >= 1 && dir[i] <= 3) // Local 1,2,3 or Global X,Y,Z (depending on CSys)
                                {
                                    mappedDir = dir[i] - 1; // Map 1,2,3 to 0,1,2
                                    loadDirectionDescription = $"{csys[i]} {(char)("X"[0] + mappedDir)}";
                                }
                                else if (dir[i] >= 4 && dir[i] <= 6) // Global X,Y,Z (non-projected)
                                {
                                    mappedDir = dir[i] - 4; // Map 4,5,6 to 0,1,2
                                    loadDirectionDescription = $"Global {(char)("X"[0] + mappedDir)}";
                                }
                                else if (dir[i] >= 7 && dir[i] <= 9) // Global X,Y,Z (projected)
                                {
                                    mappedDir = dir[i] - 7; // Map 7,8,9 to 0,1,2
                                    loadDirectionDescription = $"Global {(char)("X"[0] + mappedDir)} (Projected)";
                                }
                                else
                                {
                                    // Fallback for unexpected dir values, default to Z
                                    mappedDir = 2; // Z-direction
                                    loadDirectionDescription = "Unknown Direction (Default Z)";
                                }

                                var load = new LoadAssignment
                                {
                                    ElementID = frameName,
                                    LoadPattern = loadPatterns[i],
                                    LoadType = loadType,
                                    Value = val3[i], // *** CHANGED: Use val3 for the load value ***
                                    Unit = unit,
                                    DisplayUnits = unit,
                                    Dir = mappedDir,
                                    StartDistance = dist1[i], // These are relative distances from 0 to 1
                                    EndDistance = dist2[i],   // These are relative distances from 0 to 1
                                    RelativeDistance = (csys[i] == "Global") ? "Absolute" : "Relative", // Check coordinate system
                                    LoadCase = loadCases[i] ?? "Gravity",
                                    FormattedDescription = $"{val3[i]:F1} {unit} ({loadCases[i] ?? "Gravity"}) {loadDirectionDescription}",
                                    SourcePlatform = "Etabs",
                                    LastModified = DateTime.Now,
                                    SyncState = LoadSyncState.New
                                };

                                // If it's a varying load (val3 != val4), you might want to store val4 as well
                                // For now, we'll just use val3 as the primary 'Value' for simplicity in LoadAssignment
                                // If you need to represent the varying nature, LoadAssignment might need a 'StartValue' and 'EndValue'
                                if (Math.Abs(val3[i] - val4[i]) > 0.001) // Check if it's truly varying
                                {
                                    load.FormattedDescription = $"{val3[i]:F1} to {val4[i]:F1} {unit} ({loadCases[i] ?? "Gravity"}) {loadDirectionDescription}";
                                }

                                load.GenerateUniqueIdentifier();
                                loads.Add(load);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing frame {frameName} distributed loads: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting frame distributed loads: {ex.Message}");
            }
        }




        private void ExtractFramePointLoads(List<LoadAssignment> loads)
        {
            try
            {
                int frameCount = 0;
                string[] frameNames = null;
                _etabsModel.FrameObj.GetNameList(ref frameCount, ref frameNames);

                if (frameNames == null) return;

                foreach (string frameName in frameNames)
                {
                    try
                    {
                        int numberItems = 0;
                        string[] loadPatterns = null;
                        string[] loadCases = null;
                        int[] myType = null; // 1=Force, 2=Moment
                        string[] csys = null;
                        int[] dir = null; // 1,2,3 for Fx,Fy,Fz; 4,5,6 for Mx,My,Mz, 7,8,9 for Projected, etc.
                        double[] distance = null;
                        double[] value = null;
                        double[] val3 = null; // Added for the 'Val' parameter in some ETABS API versions

                        int ret = _etabsModel.FrameObj.GetLoadPoint(frameName, ref numberItems,
                            ref loadPatterns, ref loadCases, ref myType, ref csys, ref dir,
                            ref distance, ref value, ref val3, eItemType.Objects); // Ensure this matches your API version

                        if (ret == 0 && numberItems > 0)
                        {
                            for (int i = 0; i < numberItems; i++)
                            {
                                string loadType;
                                string unit;
                                int mappedDir;
                                string loadDirectionDescription;

                                if (myType[i] == 1) // Force load
                                {
                                    loadType = "PointLoad";
                                    unit = "kN";
                                }
                                else // Moment load (myType[i] == 2)
                                {
                                    loadType = "MomentLoad";
                                    unit = "kN-m";
                                }

                                // Determine mapped direction based on ETABS API 'dir' value
                                if (dir[i] >= 1 && dir[i] <= 3) // Local 1,2,3 or Global X,Y,Z (depending on CSys)
                                {
                                    mappedDir = dir[i] - 1; // Map 1,2,3 to 0,1,2
                                    loadDirectionDescription = $"{csys[i]} {(char)('X' + mappedDir)}";
                                }
                                else if (dir[i] >= 4 && dir[i] <= 6) // Global X,Y,Z (non-projected)
                                {
                                    mappedDir = dir[i] - 4; // Map 4,5,6 to 0,1,2
                                    loadDirectionDescription = $"Global {(char)('X' + mappedDir)}";
                                }
                                else if (dir[i] >= 7 && dir[i] <= 9) // Global X,Y,Z (projected)
                                {
                                    mappedDir = dir[i] - 7; // Map 7,8,9 to 0,1,2
                                    loadDirectionDescription = $"Global {(char)('X' + mappedDir)} (Projected)";
                                }
                                else
                                {
                                    // Fallback for unexpected dir values, default to Z
                                    mappedDir = 2; // Z-direction
                                    loadDirectionDescription = "Unknown Direction (Default Z)";
                                }

                                var load = new LoadAssignment
                                {
                                    ElementID = frameName,
                                    LoadPattern = loadPatterns[i],
                                    LoadType = loadType,
                                    Value = value[i], // Keep the sign
                                    Unit = unit,
                                    DisplayUnits = unit,
                                    Dir = mappedDir,
                                    DistanceFromStart = distance[i],
                                    LoadCase = loadCases[i] ?? "Gravity",
                                    FormattedDescription = $"{value[i]:F1} {unit} at {distance[i]:F1}m ({loadCases[i] ?? "Gravity"}) {loadDirectionDescription}",
                                    SourcePlatform = "Etabs",
                                    LastModified = DateTime.Now,
                                    SyncState = LoadSyncState.New
                                };

                                load.GenerateUniqueIdentifier();
                                loads.Add(load);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing frame {frameName} point loads: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting frame point loads: {ex.Message}");
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

                    // Refresh the ETABS view after import
                    _etabsModel.View.RefreshView();

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

                // Log the full exception for debugging
                System.Diagnostics.Debug.WriteLine($"Element import error: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                IsProcessing = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }
        #endregion

        #region Utility Methods

        /// <summary>
        /// Validates if ETABS model connection is working
        /// </summary>
        /// <returns>True if connection is valid</returns>
        private bool ValidateEtabsConnection()
        {
            try
            {
                string version = "";
                double versionNumber = 0;
                int ret = _etabsModel.GetVersion(ref version, ref versionNumber);
                return ret == 0 && !string.IsNullOrEmpty(version);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets model information for debugging purposes
        /// </summary>
        /// <returns>Model info string</returns>
        private string GetModelInfo()
        {
            try
            {
                string version = "";
                double versionNumber = 0;
                _etabsModel.GetVersion(ref version, ref versionNumber);

                return $"ETABS Version: {version} ({versionNumber})";
            }
            catch (Exception ex)
            {
                return $"Error getting model info: {ex.Message}";
            }
        }

        /// <summary>
        /// Refreshes all command can-execute states
        /// </summary>
        public void RefreshCommands()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// Debug method to test ETABS connection and log model info
        /// </summary>
        public void TestConnection()
        {
            try
            {
                if (ValidateEtabsConnection())
                {
                    var info = GetModelInfo();
                    System.Diagnostics.Debug.WriteLine($"ETABS Connection Valid: {info}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ETABS Connection Failed");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Connection test error: {ex.Message}");
            }
        }

        /// <summary>
        /// Alternative method to extract loads using try-catch for different API signatures
        /// </summary>
        public void TestLoadExtraction()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== Testing Load Extraction ===");

                // Test area loads
                int areaCount = 0;
                string[] areaNames = null;
                _etabsModel.AreaObj.GetNameList(ref areaCount, ref areaNames);
                System.Diagnostics.Debug.WriteLine($"Found {areaCount} area objects");

                if (areaNames != null && areaNames.Length > 0)
                {
                    string testArea = areaNames[0];
                    System.Diagnostics.Debug.WriteLine($"Testing with area: {testArea}");

                    // Test different API signatures
                    TestAreaLoadAPI(testArea);
                }

                // Test frame loads
                int frameCount = 0;
                string[] frameNames = null;
                _etabsModel.FrameObj.GetNameList(ref frameCount, ref frameNames);
                System.Diagnostics.Debug.WriteLine($"Found {frameCount} frame objects");

                if (frameNames != null && frameNames.Length > 0)
                {
                    string testFrame = frameNames[0];
                    System.Diagnostics.Debug.WriteLine($"Testing with frame: {testFrame}");

                    // Test different API signatures
                    TestFrameLoadAPI(testFrame);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Test extraction error: {ex.Message}");
            }
        }

        private void TestAreaLoadAPI(string areaName)
        {
            try
            {
                int numberItems = 0;
                string[] loadPatterns = null;
                string[] loadCases = null;
                string[] csys = null;
                int[] dir = null;
                double[] values = null;

                // Test without eItemType parameter
                int ret = _etabsModel.AreaObj.GetLoadUniform(areaName, ref numberItems,
                    ref loadPatterns, ref loadCases, ref csys, ref dir, ref values);

                System.Diagnostics.Debug.WriteLine($"Area API test result: {ret}, Items: {numberItems}");

                if (ret == 0 && numberItems > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Found {numberItems} uniform loads on {areaName}");
                    for (int i = 0; i < Math.Min(numberItems, 3); i++) // Log first 3 loads
                    {
                        System.Diagnostics.Debug.WriteLine($"  Load {i}: Pattern={loadPatterns[i]}, Value={values[i]}, Dir={dir[i]}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Area load API test failed: {ex.Message}");
            }
        }

        private void TestFrameLoadAPI(string frameName)
        {
            try
            {
                int numberItems = 0;
                string[] loadPatterns = null;
                string[] loadCases = null;
                int[] myType = null;
                string[] csys = null;
                int[] dir = null;
                double[] dist1 = null;
                double[] dist2 = null;
                double[] val1 = null;
                double[] val2 = null;
                double[] val3 = null;
                double[] val4 = null;

                // Test with complete signature
                int ret = _etabsModel.FrameObj.GetLoadDistributed(frameName, ref numberItems,
                    ref loadPatterns, ref loadCases, ref myType, ref csys, ref dir,
                    ref dist1, ref dist2, ref val1, ref val2, ref val3, ref val4, eItemType.Objects);

                System.Diagnostics.Debug.WriteLine($"Frame API test result: {ret}, Items: {numberItems}");

                if (ret == 0 && numberItems > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Found {numberItems} distributed loads on {frameName}");
                    for (int i = 0; i < Math.Min(numberItems, 3); i++) // Log first 3 loads
                    {
                        System.Diagnostics.Debug.WriteLine($"  Load {i}: Pattern={loadPatterns[i]}, Type={myType[i]}, Value1={val1[i]}, Dir={dir[i]}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Frame load API test failed: {ex.Message}");

                // Try a fallback approach - just skip frame loads if they don't work
                System.Diagnostics.Debug.WriteLine("Frame loads will be skipped due to API signature issues");
            }
        }

        /// <summary>
        /// Simplified load extraction that only focuses on what works
        /// </summary>
        private List<LoadAssignment> ExtractLoadsBasic()
        {
            var loads = new List<LoadAssignment>();

            try
            {
                System.Diagnostics.Debug.WriteLine("Starting basic load extraction...");

                // Only extract area uniform loads - most reliable
                int areaCount = 0;
                string[] areaNames = null;
                _etabsModel.AreaObj.GetNameList(ref areaCount, ref areaNames);

                if (areaNames != null)
                {
                    foreach (string areaName in areaNames)
                    {
                        try
                        {
                            int numberItems = 0;
                            string[] loadPatterns = null;
                            string[] loadCases = null;
                            string[] csys = null;
                            int[] dir = null;
                            double[] values = null;

                            int ret = _etabsModel.AreaObj.GetLoadUniform(areaName, ref numberItems,
                                ref loadPatterns, ref loadCases, ref csys, ref dir, ref values);

                            if (ret == 0 && numberItems > 0)
                            {
                                for (int i = 0; i < numberItems; i++)
                                {
                                    var load = new LoadAssignment
                                    {
                                        ElementID = areaName,
                                        LoadPattern = loadPatterns[i] ?? "DEAD",
                                        LoadType = "UniformLoad",
                                        Value = values[i], // Keep the sign
                                        Unit = "kN/m²",
                                        DisplayUnits = "kN/m²",
                                        Dir = (dir[i] > 0) ? dir[i] - 1 : 2, // Default to Z direction if invalid
                                        LoadCase = loadCases[i] ?? "Gravity",
                                        FormattedDescription = $"{values[i]:F1} kN/m² ({loadCases[i] ?? "Gravity"})",
                                        SourcePlatform = "Etabs",
                                        LastModified = DateTime.Now,
                                        SyncState = LoadSyncState.New
                                    };

                                    load.GenerateUniqueIdentifier();
                                    loads.Add(load);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error with area {areaName}: {ex.Message}");
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Basic extraction completed: {loads.Count} loads found");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Basic extraction error: {ex.Message}");
            }

            return loads;
        }

        #endregion
    }
}