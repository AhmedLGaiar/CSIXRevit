using EtabsGempetryExport.Model;
using EtabsGempetryExport.Model.HelperClasses;
using EtabsGempetryExport.Model.Service;
using ETABSv1;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;

namespace EtabsGempetryExport.Plugins
{

    public class cPlugin : cPluginContract
    {
        public void Main(ref cSapModel SapModel, ref cPluginCallback ISapPlugin)
        {
            try
            {
                // Register assembly resolver for loading WPF libraries
                AppDomain.CurrentDomain.AssemblyResolve += ResolveStyleLibrary;

                // Store the SapModel in a local variable to avoid using ref in lambdas
                cSapModel etabsModel = SapModel;

                // Create and show the unified view directly
                var unifiedView = new UnifiedView(etabsModel);
                _ = unifiedView.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error in plugin: {ex.Message}\n\n{ex.StackTrace}", "Plugin Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ISapPlugin.Finish(0);
            }
        }

        public int Info(ref string Text)
        {
            Text = "Revit-ETABS Integration Plugin - Unified Interface";
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
