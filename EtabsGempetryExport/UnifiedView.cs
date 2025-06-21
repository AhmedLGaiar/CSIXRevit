using ETABSv1;
using System.Windows;

namespace EtabsGempetryExport.Plugins
{
    internal class UnifiedView : Window
    {
        private cSapModel etabsModel;

        public UnifiedView(cSapModel etabsModel)
        {
            this.etabsModel = etabsModel;
            // Initialize the window properties and content here
        }
    }
}