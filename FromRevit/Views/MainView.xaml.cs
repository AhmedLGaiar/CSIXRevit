using FromRevit.ViewModels;
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

namespace FromRevit.Views
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : Window
    {
        private readonly MainViewViewModel _mv;

        public MainView(MainViewViewModel mv)
        {
            _mv = mv;
            DataContext= _mv;
            InitializeComponent();
        }
        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var windowWidth = this.Width;

            this.Left = screenWidth - windowWidth-30;
        }
    }
}
