using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.DependencyInjection;
using ExportJsonFileFromRevit.Services;
using ExportJsonFileFromRevit.ViewModels;
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

namespace ExportJsonFileFromRevit.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(Document doc)
        {
            InitializeComponent();
            DataContext = new MainViewModel(new FileService(), new RevitService(), doc);
        }
    }
}
