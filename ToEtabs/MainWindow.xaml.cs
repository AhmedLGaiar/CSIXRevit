using System.Windows;
using ToEtabs.ViewModels;

namespace ToEtabs
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _dataContext;
        public MainWindow(MainWindowViewModel mainWindowViewModel)
        {
            _dataContext = mainWindowViewModel;
            DataContext = mainWindowViewModel;
            InitializeComponent();
        }
    }
}