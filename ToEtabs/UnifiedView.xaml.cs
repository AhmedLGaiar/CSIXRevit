using System.Windows;
using ETABSv1;
using ToEtabs.ViewModels;
using System.ComponentModel;

namespace ToEtabs.Views
{
    public partial class UnifiedView : Window
    {
        private UnifiedViewModel _viewModel;

        public UnifiedView(cSapModel etabsModel)
        {
            InitializeComponent();
            _viewModel = new UnifiedViewModel(etabsModel);
            DataContext = _viewModel;

            // Subscribe to property changes to handle visibility manually
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.ImportLoadSummary))
            {
                // Handle load preview visibility
                if (string.IsNullOrEmpty(_viewModel.ImportLoadSummary))
                {
                    LoadPreviewBorder.Visibility = Visibility.Collapsed;
                }
                else
                {
                    LoadPreviewBorder.Visibility = Visibility.Visible;
                }
            }
        }

        // Browse button handlers
        private void BrowseImportButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.BrowseImportFile();
        }

        private void BrowseExportButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.BrowseExportFile();
        }

        private void BrowseElementsButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.BrowseImportFile(); // Same method, different button for clarity
        }

        // Main action button handlers
        private void ImportLoadsButton_Click(object sender, RoutedEventArgs e)
        {
            // Use the regular load import method (removed the Simple version)
            _viewModel.PerformLoadImport();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.PerformLoadExport();
        }

        private void ImportElementsButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.PerformElementImport();
        }
    }
}