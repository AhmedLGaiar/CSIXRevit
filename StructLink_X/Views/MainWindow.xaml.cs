using Autodesk.Revit.DB;
using StructLink_X.ViewModels;
using System.Windows;
using HelixToolkit.Wpf;

namespace StructLink_X.Views
{
    public partial class MainWindow : Window
    {
        private readonly Document _doc;
        private MainViewModel _viewModel;

        public MainWindow(Document doc)
        {
            try
            {
                _doc = doc ?? throw new ArgumentNullException(nameof(doc));

                InitializeComponent();

                // Initialize ViewModel with error handling
                _viewModel = new MainViewModel(_doc);
                DataContext = _viewModel;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل النافذة: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                throw; // Re-throw to prevent partially initialized window
            }
        }

        private void Viewport3D_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel != null && sender is HelixViewport3D viewport)
                {
                    _viewModel.Viewport3D = viewport;

                    // Check if the command can execute before executing
                    if (_viewModel.Update3DViewCommand?.CanExecute(null) == true)
                    {
                        _viewModel.Update3DViewCommand.Execute(null);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash the application
                _viewModel?.UpdateStatusMessage($"خطأ في تحميل العرض الثري دي: {ex.Message}");
            }
        }

        // Override to handle window closing gracefully
        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Clean up resources if needed
                _viewModel?.Cleanup();
            }
            catch (Exception ex)
            {
                // Log but don't throw on cleanup
                System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
            }
            finally
            {
                base.OnClosed(e);
            }
        }
    }
}