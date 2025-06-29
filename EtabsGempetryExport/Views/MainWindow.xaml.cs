using EtabsGeometryExport.Model.Service;
using EtabsGeometryExport.ViewModel;
using System.Windows;

namespace EtabsGeometryExport.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // Constructor that accepts ViewModel (preferred approach)
        public MainWindow(MainViewModel viewModel) : this()
        {
            DataContext = viewModel;

            // Subscribe to ViewModel events for better UX
            if (viewModel != null)
            {
                viewModel.ShowError += OnShowError;
                viewModel.ShowSuccess += OnShowSuccess;
            }
        }

        // Alternative: Property to set DataContext
        public MainViewModel ViewModel
        {
            get => DataContext as MainViewModel;
            set
            {
                if (DataContext is MainViewModel oldViewModel)
                {
                    oldViewModel.ShowError -= OnShowError;
                    oldViewModel.ShowSuccess -= OnShowSuccess;
                }

                DataContext = value;

                if (value != null)
                {
                    value.ShowError += OnShowError;
                    value.ShowSuccess += OnShowSuccess;
                }
            }
        }

        // Handle error messages
        private void OnShowError(object sender, string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // Handle success messages
        private void OnShowSuccess(object sender, string message)
        {
            MessageBox.Show(message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Clean up when window closes
        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.ShowError -= OnShowError;
                viewModel.ShowSuccess -= OnShowSuccess;
                viewModel.Dispose();
            }
            base.OnClosed(e);
        }

        // Alternative static factory method
        public static MainWindow CreateWithServices(IETABSService etabsService, IFileService fileService)
        {
            var viewModel = new MainViewModel(etabsService, fileService);
            return new MainWindow(viewModel);
        }
    }
}