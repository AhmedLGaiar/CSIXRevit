using StructLink_X._0.Models;
using StructLink_X._0.ViewModels;
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

namespace StructLink_X._0.Views
{
    /// <summary>
    /// Interaction logic for RebarEditor.xaml
    /// </summary>
    public partial class RebarEditor : Window
    {
       
            public RebarEditor(IEnumerable<ColumnRCData> columns, IEnumerable<BeamRCData> beams)
            {
                InitializeComponent();
                DataContext = new RebarEditorViewModel(columns, beams);
            }
        }
    }

