using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using EtabsGempetryExport.Model.Service;

namespace EtabsGempetryExport.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IETABSService _etabsService;
        private readonly IFileService _fileService;

        private string _statusText = "Ready to extract data from ETABS...";
        private bool _isExtracting = false;
        private int _beamCount = 0;
        private int _columnCount = 0;
        private int _slabCount = 0;
        private int _wallCount = 0; // Added wall count
        private string _lastSavedFile = string.Empty;

        private bool _exportBeams = true;
        private bool _exportColumns = true;
        private bool _exportSlabs = true;
        private bool _exportWalls = true; // Added wall export option

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public bool IsExtracting
        {
            get => _isExtracting;
            set
            {
                if (SetProperty(ref _isExtracting, value))
                {
                    ((RelayCommand)ExtractDataCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public int BeamCount
        {
            get => _beamCount;
            set => SetProperty(ref _beamCount, value);
        }

        public int ColumnCount
        {
            get => _columnCount;
            set => SetProperty(ref _columnCount, value);
        }

        public int SlabCount
        {
            get => _slabCount;
            set => SetProperty(ref _slabCount, value);
        }

        public int WallCount // Added wall count property
        {
            get => _wallCount;
            set => SetProperty(ref _wallCount, value);
        }

        public string LastSavedFile
        {
            get => _lastSavedFile;
            set => SetProperty(ref _lastSavedFile, value);
        }

        public bool ExportBeams
        {
            get => _exportBeams;
            set => SetProperty(ref _exportBeams, value);
        }

        public bool ExportColumns
        {
            get => _exportColumns;
            set => SetProperty(ref _exportColumns, value);
        }

        public bool ExportSlabs
        {
            get => _exportSlabs;
            set => SetProperty(ref _exportSlabs, value);
        }

        public bool ExportWalls // Added wall export property
        {
            get => _exportWalls;
            set => SetProperty(ref _exportWalls, value);
        }

        public ICommand ExtractDataCommand { get; }

        public event EventHandler<string> ShowError;
        public event EventHandler<string> ShowSuccess;

        public MainViewModel() : this(new ETABSService(), new FileService()) { }

        public MainViewModel(IETABSService etabsService, IFileService fileService)
        {
            _etabsService = etabsService ?? throw new ArgumentNullException(nameof(etabsService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));

            ExtractDataCommand = new RelayCommand(async () => await ExtractDataAsync(), CanExtractData);
        }

        private async Task ExtractDataAsync()
        {
            try
            {
                IsExtracting = true;
                StatusText = "Connecting to ETABS...";

                if (!_etabsService.ConnectToETABS())
                {
                    var errorMessage = "No running ETABS instance found!\nPlease open your ETABS model first.";
                    ShowError?.Invoke(this, errorMessage);
                    StatusText = "Connection failed. Please open ETABS first.";
                    return;
                }

                StatusText = "Extracting structural data...";
                var structuralData = await _etabsService.ExtractAllDataAsync();

                // Clear data based on export settings
                if (!ExportBeams)
                    structuralData.Beams.Clear();

                if (!ExportColumns)
                    structuralData.Columns.Clear();

                if (!ExportSlabs)
                    structuralData.Slabs.Clear();

                if (!ExportWalls) // Added wall clearing logic
                    structuralData.StructWalls.Clear();

                // Update counts
                BeamCount = structuralData.Beams.Count;
                ColumnCount = structuralData.Columns.Count;
                SlabCount = structuralData.Slabs.Count;
                WallCount = structuralData.StructWalls.Count; // Added wall count update

                // Updated status message to include walls
                StatusText = $"Found {BeamCount} beams, {ColumnCount} columns, {SlabCount} slabs, and {WallCount} walls. Choose save location...";

                if (_fileService.SaveWithDialog(structuralData, out string filePath))
                {
                    LastSavedFile = Path.GetFileName(filePath);
                    StatusText = $"Successfully saved {BeamCount} beams, {ColumnCount} columns, {SlabCount} slabs, and {WallCount} walls to {LastSavedFile}";

                    // Updated success message to include walls
                    var successMessage = $"Data extracted successfully!\n\n" +
                                         $"Beams: {BeamCount}\n" +
                                         $"Columns: {ColumnCount}\n" +
                                         $"Slabs: {SlabCount}\n" +
                                         $"Walls: {WallCount}\n" +
                                         $"Saved to: {LastSavedFile}";

                    ShowSuccess?.Invoke(this, successMessage);
                }
                else
                {
                    StatusText = "Save operation cancelled.";
                }
            }
            catch (Exception ex)
            {
                StatusText = "Error occurred during extraction.";
                ShowError?.Invoke(this, $"Error: {ex.Message}");
            }
            finally
            {
                IsExtracting = false;
            }
        }

        private bool CanExtractData() => !IsExtracting;

        public void Dispose()
        {
            _etabsService?.Dispose();
        }

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }

    #region RelayCommand Helper Class

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object parameter)
        {
            _execute();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    #endregion
}