using FromRevit.ViewModels;
using System.Windows;

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
            DataContext = _mv;
            InitializeComponent();
        }
        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var windowWidth = this.Width;

            this.Left = screenWidth - windowWidth - 30;
        }
    }
}
