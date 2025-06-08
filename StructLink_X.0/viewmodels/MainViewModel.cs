using StructLink_X.Models;
using StructLink_X.Services;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using Autodesk.Revit.DB;
using HelixToolkit.Wpf;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using StructLink_X.Views;
using OfficeOpenXml;

namespace StructLink_X.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        #region Private Fields
        private readonly Document _doc;
        private readonly Dispatcher _dispatcher;

        // Collections
        private ObservableCollection<ColumnRCData> _columns;
        private ObservableCollection<BeamRCData> _beams;
        private ObservableCollection<ColumnRCData> _filteredColumns;
        private ObservableCollection<BeamRCData> _filteredBeams;

        // Selection and Display
        private string _selectedCode;
        private string _statusMessage;
        private ColumnRCData _selectedColumn;
        private BeamRCData _selectedBeam;
        private BitmapSource _sectionImage;
        private HelixViewport3D _viewport3D;

        // UI State
        private bool _isLoading;
        private bool _is3DViewVisible;
        private bool _is2DViewVisible;
        private string _searchText;
        private ViewMode _currentViewMode;

        // Statistics
        private int _compliantColumnsCount;
        private int _nonCompliantColumnsCount;
        private int _compliantBeamsCount;
        private int _nonCompliantBeamsCount;

        // Commands
        private ICommand _loadJsonCommand;
        private ICommand _exportToPdfCommand;
        private ICommand _exportToExcelCommand;
        private ICommand _generateRebarCommand;
        private ICommand _editRebarCommand;
        private ICommand _update3DViewCommand;
        private ICommand _refreshViewCommand;
        private ICommand _clearSearchCommand;
        private ICommand _showComplianceReportCommand;
        private ICommand _exportReportCommand;
        private ICommand _validateAllCommand;
        #endregion

        #region Public Properties
        // Collections
        public ObservableCollection<ColumnRCData> Columns
        {
            get => _columns;
            set
            {
                _columns = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalElementsCount));
                OnPropertyChanged(nameof(CompliancePercentage));
                UpdateFilteredCollections();
                UpdateStatistics();
            }
        }

        public ObservableCollection<BeamRCData> Beams
        {
            get => _beams;
            set
            {
                _beams = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalElementsCount));
                OnPropertyChanged(nameof(CompliancePercentage));
                UpdateFilteredCollections();
                UpdateStatistics();
            }
        }

        public ObservableCollection<ColumnRCData> FilteredColumns
        {
            get => _filteredColumns;
            set { _filteredColumns = value; OnPropertyChanged(); }
        }

        public ObservableCollection<BeamRCData> FilteredBeams
        {
            get => _filteredBeams;
            set { _filteredBeams = value; OnPropertyChanged(); }
        }

        // Selection and Display
        public string SelectedCode
        {
            get => _selectedCode;
            set
            {
                _selectedCode = value;
                OnPropertyChanged();
                UpdateStatistics();
                RefreshCurrentView();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public ColumnRCData SelectedColumn
        {
            get => _selectedColumn;
            set
            {
                _selectedColumn = value;
                OnPropertyChanged();
                if (_selectedColumn != null)
                {
                    UpdateColumnVisualization();
                    StatusMessage = $"Displaying Column: {_selectedColumn.SectionName} - " +
                                  $"Dimensions: {_selectedColumn.Width}x{_selectedColumn.Depth}mm";
                }
            }
        }

        public BeamRCData SelectedBeam
        {
            get => _selectedBeam;
            set
            {
                _selectedBeam = value;
                OnPropertyChanged();
                if (_selectedBeam != null)
                {
                    UpdateBeamVisualization();
                    StatusMessage = $"Displaying Beam: {_selectedBeam.UniqueName} - " +
                                  $"Dimensions: {_selectedBeam.Width}x{_selectedBeam.Depth}mm";
                }
            }
        }

        public BitmapSource SectionImage
        {
            get => _sectionImage;
            set { _sectionImage = value; OnPropertyChanged(); }
        }

        public HelixViewport3D Viewport3D
        {
            get => _viewport3D;
            set { _viewport3D = value; OnPropertyChanged(); }
        }

        // UI State
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public bool Is3DViewVisible
        {
            get => _is3DViewVisible;
            set { _is3DViewVisible = value; OnPropertyChanged(); }
        }

        public bool Is2DViewVisible
        {
            get => _is2DViewVisible;
            set { _is2DViewVisible = value; OnPropertyChanged(); }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                UpdateFilteredCollections();
            }
        }

        public ViewMode CurrentViewMode
        {
            get => _currentViewMode;
            set
            {
                _currentViewMode = value;
                OnPropertyChanged();
                UpdateViewVisibility();
            }
        }

        // Statistics
        public int CompliantColumnsCount
        {
            get => _compliantColumnsCount;
            set
            {
                _compliantColumnsCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CompliancePercentage));
            }
        }

        public int NonCompliantColumnsCount
        {
            get => _nonCompliantColumnsCount;
            set
            {
                _nonCompliantColumnsCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CompliancePercentage));
            }
        }

        public int CompliantBeamsCount
        {
            get => _compliantBeamsCount;
            set
            {
                _compliantBeamsCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CompliancePercentage));
            }
        }

        public int NonCompliantBeamsCount
        {
            get => _nonCompliantBeamsCount;
            set
            {
                _nonCompliantBeamsCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CompliancePercentage));
            }
        }

        public int TotalElementsCount
        {
            get => (Columns?.Count ?? 0) + (Beams?.Count ?? 0);
        }

        public double CompliancePercentage
        {
            get => TotalElementsCount > 0 ?
                ((double)(CompliantColumnsCount + CompliantBeamsCount) / TotalElementsCount) * 100 : 0;
        }

        // Added: Property to indicate if there are no compliant elements
        public string ComplianceDisplayText
        {
            get
            {
                if (TotalElementsCount == 0)
                    return "No elements to evaluate";
                if (CompliantColumnsCount + CompliantBeamsCount == 0)
                    return "No compliant elements";
                return $"{CompliancePercentage:F1}%";
            }
        }

        // Static Collections
        public List<string> Codes => new List<string>
        {
            "EgyptianCode",
            "SaudiCode",
            "ACI318",
            "Eurocode2"
        };

        public List<ViewMode> ViewModes => new List<ViewMode>
        {
            ViewMode.View2D,
            ViewMode.View3D,
            ViewMode.Both
        };

        // Commands
        public ICommand LoadJsonCommand
        {
            get => _loadJsonCommand;
            private set => _loadJsonCommand = value;
        }

        public ICommand ExportToPdfCommand
        {
            get => _exportToPdfCommand;
            private set => _exportToPdfCommand = value;
        }

        public ICommand ExportToExcelCommand
        {
            get => _exportToExcelCommand;
            private set => _exportToExcelCommand = value;
        }

        public ICommand GenerateRebarCommand
        {
            get => _generateRebarCommand;
            private set => _generateRebarCommand = value;
        }

        public ICommand EditRebarCommand
        {
            get => _editRebarCommand;
            private set => _editRebarCommand = value;
        }

        public ICommand Update3DViewCommand
        {
            get => _update3DViewCommand;
            private set => _update3DViewCommand = value;
        }

        public ICommand RefreshViewCommand
        {
            get => _refreshViewCommand;
            private set => _refreshViewCommand = value;
        }

        public ICommand ClearSearchCommand
        {
            get => _clearSearchCommand;
            private set => _clearSearchCommand = value;
        }

        public ICommand ShowComplianceReportCommand
        {
            get => _showComplianceReportCommand;
            private set => _showComplianceReportCommand = value;
        }

        public ICommand ExportReportCommand
        {
            get => _exportReportCommand;
            private set => _exportReportCommand = value;
        }

        public ICommand ValidateAllCommand
        {
            get => _validateAllCommand;
            private set => _validateAllCommand = value;
        }
        #endregion

        #region Constructor
        public MainViewModel(Document doc)
        {
            _doc = doc;
            _dispatcher = Dispatcher.CurrentDispatcher;

            InitializeCollections();
            InitializeDefaults();
            InitializeCommands();
        }

        private void InitializeCollections()
        {
            Columns = new ObservableCollection<ColumnRCData>();
            Beams = new ObservableCollection<BeamRCData>();
            FilteredColumns = new ObservableCollection<ColumnRCData>();
            FilteredBeams = new ObservableCollection<BeamRCData>();
        }

        private void InitializeDefaults()
        {
            SelectedCode = "EgyptianCode";
            CurrentViewMode = ViewMode.View2D;
            SearchText = string.Empty;
            StatusMessage = "Ready to start - Load a JSON file to begin";
        }

        private void InitializeCommands()
        {
            _loadJsonCommand = new RelayCommand(LoadJson, CanExecuteFileOperations);
            _exportToPdfCommand = new RelayCommand(ExportToPdf, CanExport);
            _exportToExcelCommand = new RelayCommand(ExportToExcel, CanExport);
            _generateRebarCommand = new RelayCommand(GenerateRebar, CanGenerateRebar);
            _editRebarCommand = new RelayCommand(EditRebar, CanEditRebar);
            _update3DViewCommand = new RelayCommand(Update3DView, CanUpdate3DView);
            _refreshViewCommand = new RelayCommand(RefreshView, CanRefreshView);
            _clearSearchCommand = new RelayCommand(ClearSearch, CanClearSearch);
            _showComplianceReportCommand = new RelayCommand(ShowComplianceReport, CanShowReport);
            _exportReportCommand = new RelayCommand(ExportReport, CanExport);
            _validateAllCommand = new RelayCommand(ValidateAll, CanValidate);
        }
        #endregion

        #region Command Implementations
        private async void LoadJson(object parameter)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Select JSON File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                IsLoading = true;
                StatusMessage = "Loading data...";

                try
                {
                    await Task.Run(() => LoadJsonFile(openFileDialog.FileName));
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error loading file: {ex.Message}";
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private void LoadJsonFile(string fileName)
        {
            try
            {
                string json = System.IO.File.ReadAllText(fileName);
                json = json.Trim();

                if (!IsValidJson(json))
                {
                    _dispatcher.Invoke(() => StatusMessage = "Error: The selected file is not a valid JSON file");
                    return;
                }

                var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                _dispatcher.Invoke(() =>
                {
                    LoadColumnsFromJson(data);
                    LoadBeamsFromJson(data);

                    string fileName_short = System.IO.Path.GetFileName(fileName);
                    StatusMessage = $"Data loaded successfully from {fileName_short} - " +
                                  $"Columns: {Columns.Count}, Beams: {Beams.Count}";
                });
            }
            catch (Exception ex)
            {
                _dispatcher.Invoke(() => StatusMessage = $"Error loading file: {ex.Message}");
            }
        }

        private bool IsValidJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || (!json.StartsWith("{") && !json.StartsWith("[")))
                return false;

            try
            {
                JsonConvert.DeserializeObject(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void LoadColumnsFromJson(Dictionary<string, object> data)
        {
            if (data.ContainsKey("columnRCDatas"))
            {
                var columnsJson = JsonConvert.SerializeObject(data["columnRCDatas"]);
                var loadedColumns = JsonConvert.DeserializeObject<List<ColumnRCData>>(columnsJson);
                Columns = new ObservableCollection<ColumnRCData>(loadedColumns ?? new List<ColumnRCData>());
            }
            else
            {
                Columns = new ObservableCollection<ColumnRCData>();
            }
        }

        private void LoadBeamsFromJson(Dictionary<string, object> data)
        {
            if (data.ContainsKey("beamRCDatas"))
            {
                var beamsJson = JsonConvert.SerializeObject(data["beamRCDatas"]);
                var loadedBeams = JsonConvert.DeserializeObject<List<BeamRCData>>(beamsJson);
                Beams = new ObservableCollection<BeamRCData>(loadedBeams ?? new List<BeamRCData>());
            }
            else
            {
                Beams = new ObservableCollection<BeamRCData>();
            }
        }

        private async void ExportToPdf(object parameter)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                Title = "Save Report as PDF"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                IsLoading = true;
                StatusMessage = "Generating PDF file...";

                try
                {
                    await Task.Run(() => PdfExporter.ExportToPDF(Columns, Beams, saveFileDialog.FileName, SelectedCode));
                    StatusMessage = "PDF exported successfully!";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error exporting PDF: {ex.Message}";
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private async void ExportToExcel(object parameter)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx",
                Title = "Save Data as Excel"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                IsLoading = true;
                StatusMessage = "Generating Excel file...";

                try
                {
                    ExcelExporter excelExporter = new ExcelExporter();
                    await Task.Run(() => excelExporter.ExportToExcel(Columns, Beams, saveFileDialog.FileName));
                    StatusMessage = "Excel exported successfully!";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error exporting Excel: {ex.Message}. Please ensure EPPlus license is set correctly.";
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private async void GenerateRebar(object parameter)
        {
            IsLoading = true;
            StatusMessage = "Generating rebar in Revit...";

            try
            {
                await Task.Run(() => GenerateRebarInRevit());
                StatusMessage = "Rebar generated successfully in Revit!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error generating rebar: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void GenerateRebarInRevit()
        {
            RebarGenerator generator = new RebarGenerator(_doc);

            var columnCollector = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_StructuralColumns)
                .WhereElementIsNotElementType()
                .ToElements();

            var beamCollector = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .WhereElementIsNotElementType()
                .ToElements();

            foreach (var column in Columns)
            {
                Element hostColumn = columnCollector.FirstOrDefault(e =>
                {
                    var param = e.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
                    return e.Name == column.SectionName || (param != null && param.AsString() == column.SectionName);
                });

                if (hostColumn != null)
                    generator.GenerateColumnRebar(column, hostColumn);
            }

            foreach (var beam in Beams)
            {
                Element hostBeam = beamCollector.FirstOrDefault(e =>
                {
                    var param = e.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
                    return e.Name == beam.UniqueName || (param != null && param.AsString() == beam.UniqueName);
                });

                if (hostBeam != null)
                    generator.GenerateBeamRebar(beam, hostBeam);
            }
        }

        private void EditRebar(object parameter)
        {
            try
            {
                // Pass the required arguments to the RebarEditor constructor
                RebarEditor editor = new RebarEditor(Columns, Beams);
                RebarEditorViewModel editorViewModel = new RebarEditorViewModel(Columns, Beams);
                editor.DataContext = editorViewModel;

                if (editor.ShowDialog() == true)
                {
                    Columns = new ObservableCollection<ColumnRCData>(editorViewModel.Columns);
                    Beams = new ObservableCollection<BeamRCData>(editorViewModel.Beams);
                    StatusMessage = "Rebar data updated successfully!";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error editing rebar: {ex.Message}";
            }
        }

        private void Update3DView(object parameter)
        {
            try
            {
                if (Viewport3D == null)
                {
                    StatusMessage = "3D view is not available";
                    return;
                }

                _dispatcher.Invoke(() => Viewport3D.Children?.Clear());

                if (SelectedColumn != null)
                {
                    VisualizationService.DrawColumn3D(Viewport3D, SelectedColumn, SelectedCode ?? "EgyptianCode");
                    StatusMessage = $"3D view updated for column: {SelectedColumn.SectionName}";
                }
                else if (SelectedBeam != null)
                {
                    VisualizationService.DrawBeam3D(Viewport3D, SelectedBeam, SelectedCode ?? "EgyptianCode");
                    StatusMessage = $"3D view updated for beam: {SelectedBeam.UniqueName}";
                }
                else
                {
                    StatusMessage = "No element selected for display";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error updating 3D view: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"3D View Error: {ex}");
            }
        }

        private void RefreshView(object parameter)
        {
            RefreshCurrentView();
            StatusMessage = "View refreshed";
        }

        private void ClearSearch(object parameter)
        {
            SearchText = string.Empty;
            StatusMessage = "Search cleared";
        }

        private void ShowComplianceReport(object parameter)
        {
            MessageBox.Show($"Compliance Report:\n" +
                          $"Compliant Columns: {CompliantColumnsCount}\n" +
                          $"Non-Compliant Columns: {NonCompliantColumnsCount}\n" +
                          $"Compliant Beams: {CompliantBeamsCount}\n" +
                          $"Non-Compliant Beams: {NonCompliantBeamsCount}\n" +
                          $"Compliance Rate: {CompliancePercentage:F1}%",
                          "Compliance Report", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void ExportReport(object parameter)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|PDF files (*.pdf)|*.pdf",
                Title = "Export Compliance Report"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                IsLoading = true;
                StatusMessage = "Generating report...";

                try
                {
                    await Task.Run(() => GenerateComplianceReport(saveFileDialog.FileName));
                    StatusMessage = "Report exported successfully!";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error exporting report: {ex.Message}";
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private async void ValidateAll(object parameter)
        {
            IsLoading = true;
            StatusMessage = "Checking compliance...";

            try
            {
                await Task.Run(UpdateStatistics);
                StatusMessage = $"Validated {TotalElementsCount} elements - Compliance rate: {CompliancePercentage:F1}%";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error during validation: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
        #endregion

        #region Command Can Execute Methods
        private bool CanExecuteFileOperations(object parameter) => !IsLoading;
        private bool CanExport(object parameter) => !IsLoading && TotalElementsCount > 0;
        private bool CanGenerateRebar(object parameter) => !IsLoading && _doc != null && TotalElementsCount > 0;
        private bool CanEditRebar(object parameter) => !IsLoading && TotalElementsCount > 0;
        private bool CanUpdate3DView(object parameter)
        {
            return !IsLoading &&
                   Viewport3D != null &&
                   (SelectedColumn != null || SelectedBeam != null) &&
                   !string.IsNullOrEmpty(SelectedCode);
        }
        private bool CanRefreshView(object parameter) => !IsLoading;
        private bool CanClearSearch(object parameter) => !string.IsNullOrEmpty(SearchText);
        private bool CanShowReport(object parameter) => TotalElementsCount > 0;
        private bool CanValidate(object parameter) => !IsLoading && TotalElementsCount > 0;
        #endregion

        #region Helper Methods
        private void UpdateFilteredCollections()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredColumns = new ObservableCollection<ColumnRCData>(Columns ?? new ObservableCollection<ColumnRCData>());
                FilteredBeams = new ObservableCollection<BeamRCData>(Beams ?? new ObservableCollection<BeamRCData>());
            }
            else
            {
                var filteredColumns = Columns?.Where(c =>
                    c.SectionName?.ToLower().Contains(SearchText.ToLower()) == true ||
                    c.UniqueName.ToString().ToLower().Contains(SearchText.ToLower()) == true) ?? Enumerable.Empty<ColumnRCData>();

                var filteredBeams = Beams?.Where(b =>
                    b.UniqueName?.ToLower().Contains(SearchText.ToLower()) == true ||
                    b.SectionName?.ToLower().Contains(SearchText.ToLower()) == true) ?? Enumerable.Empty<BeamRCData>();

                FilteredColumns = new ObservableCollection<ColumnRCData>(filteredColumns);
                FilteredBeams = new ObservableCollection<BeamRCData>(filteredBeams);
            }
        }

        private void UpdateStatistics()
        {
            if (Columns == null || Beams == null || string.IsNullOrEmpty(SelectedCode))
            {
                CompliantColumnsCount = NonCompliantColumnsCount = CompliantBeamsCount = NonCompliantBeamsCount = 0;
                OnPropertyChanged(nameof(CompliancePercentage));
                OnPropertyChanged(nameof(TotalElementsCount));
                OnPropertyChanged(nameof(ComplianceDisplayText));
                System.Diagnostics.Debug.WriteLine($"UpdateStatistics: Early return - Columns: {Columns?.Count ?? 0}, Beams: {Beams?.Count ?? 0}, SelectedCode: {SelectedCode}");
                return;
            }

           
           
            NonCompliantBeamsCount = Beams.Count - CompliantBeamsCount;

            System.Diagnostics.Debug.WriteLine($"UpdateStatistics: TotalElements: {TotalElementsCount}, CompliantColumns: {CompliantColumnsCount}, NonCompliantColumns: {NonCompliantColumnsCount}, CompliantBeams: {CompliantBeamsCount}, NonCompliantBeams: {NonCompliantBeamsCount}, CompliancePercentage: {CompliancePercentage:F1}%");

            OnPropertyChanged(nameof(CompliancePercentage));
            OnPropertyChanged(nameof(TotalElementsCount));
            OnPropertyChanged(nameof(ComplianceDisplayText));
        }

        private void UpdateColumnVisualization()
        {
            if (SelectedColumn == null) return;

            try
            {
                if (CurrentViewMode == ViewMode.View2D || CurrentViewMode == ViewMode.Both)
                {
                    SectionImage = VisualizationService.DrawColumn2D(SelectedColumn, SelectedCode);
                }

                if ((CurrentViewMode == ViewMode.View3D || CurrentViewMode == ViewMode.Both) && Viewport3D != null)
                {
                    VisualizationService.DrawColumn3D(Viewport3D, SelectedColumn, SelectedCode);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error updating column visualization: {ex.Message}";
            }
        }

        private void UpdateBeamVisualization()
        {
            if (SelectedBeam == null) return;

            try
            {
                if (CurrentViewMode == ViewMode.View2D || CurrentViewMode == ViewMode.Both)
                {
                    SectionImage = VisualizationService.DrawBeam2D(SelectedBeam, SelectedCode);
                }

                if ((CurrentViewMode == ViewMode.View3D || CurrentViewMode == ViewMode.Both) && Viewport3D != null)
                {
                    VisualizationService.DrawBeam3D(Viewport3D, SelectedBeam, SelectedCode);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error updating beam visualization: {ex.Message}";
            }
        }

        public void Cleanup()
        {
            try
            {
                if (Viewport3D != null)
                {
                    Viewport3D.Children?.Clear();
                }

                Columns?.Clear();
                Beams?.Clear();
                FilteredColumns?.Clear();
                FilteredBeams?.Clear();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cleanup error: {ex.Message}");
            }
        }

        private void RefreshCurrentView()
        {
            if (SelectedColumn != null)
            {
                UpdateColumnVisualization();
            }
            else if (SelectedBeam != null)
            {
                UpdateBeamVisualization();
            }
        }

        private void UpdateViewVisibility()
        {
            Is2DViewVisible = CurrentViewMode == ViewMode.View2D || CurrentViewMode == ViewMode.Both;
            Is3DViewVisible = CurrentViewMode == ViewMode.View3D || CurrentViewMode == ViewMode.Both;

            RefreshCurrentView();
        }

        private void GenerateComplianceReport(string fileName)
        {
            var report = $"Compliance Report for Code {SelectedCode}\n" +
                        $"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n" +
                        $"Statistics Summary:\n" +
                        $"Total Elements: {TotalElementsCount}\n" +
                        $"Compliant Columns: {CompliantColumnsCount}\n" +
                        $"Non-Compliant Columns: {NonCompliantColumnsCount}\n" +
                        $"Compliant Beams: {CompliantBeamsCount}\n" +
                        $"Non-Compliant Beams: {NonCompliantBeamsCount}\n" +
                        $"Overall Compliance Rate: {CompliancePercentage:F2}%\n";

            System.IO.File.WriteAllText(fileName, report);
        }

        internal void UpdateStatusMessage(string v)
        {
            StatusMessage = v;
        }
        #endregion
    }

    public enum ViewMode
    {
        View2D,
        View3D,
        Both
    }
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object parameter)
        {
            if (CanExecute(parameter))
                _execute(parameter);
        }
    }


}