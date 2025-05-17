using System.IO;
using ETABSv1;
using System.Reflection;
using ToEtabs.ViewModels;

namespace ToEtabs
{
    public class cPlugin : cPluginContract
    {
        public void Main(ref cSapModel SapModel, ref cPluginCallback ISapPlugin)
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveStyleLibrary;

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
        private static Assembly ResolveStyleLibrary(object sender, ResolveEventArgs args)
        {
            var requestedAssembly = new AssemblyName(args.Name).Name + ".dll";
            var pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var fullPath = Path.Combine(pluginDir, requestedAssembly);

            return File.Exists(fullPath) ? Assembly.LoadFrom(fullPath) : null;
        }
    }
}
