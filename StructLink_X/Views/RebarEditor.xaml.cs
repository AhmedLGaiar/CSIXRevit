using StructLink_X.Models;
using StructLink_X.ViewModels;
using System.Windows;


namespace StructLink_X.Views
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

