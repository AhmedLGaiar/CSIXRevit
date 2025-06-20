using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Auto_Sheets.ViewModels;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Auto_Sheets.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private UIDocument _uiDocument;

        public MainWindow(UIDocument uidoc)
        {
            InitializeComponent();
            _uiDocument = uidoc;

            var viewModel = new MainViewModel(
                _uiDocument,
                refreshAction: RefreshWindow,
                closeAction: CloseWindow
            );

            DataContext = viewModel;
        }

        private void RefreshWindow()
        {
            this.Close();
        }

        private void CloseWindow()
        {
            this.Close();
        }
    }

}
