using ETABSv1;
using ToEtabs.ViewModels;

namespace ToEtabs
{
    public class cPlugin : cPluginContract
    {
        public void Main(ref cSapModel SapModel, ref cPluginCallback ISapPlugin)
        {
            MainWindowViewModel ViewModel = new MainWindowViewModel(SapModel);
            MainWindow mainWindow = new MainWindow(ViewModel);
            mainWindow.ShowDialog();

            ISapPlugin.Finish(0);
        }

        public int Info(ref string Text)
        {
           Text = "ToEtabs Plugin From ELGaiar";
            return 0;
        }
    }
}
