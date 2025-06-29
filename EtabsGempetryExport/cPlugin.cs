using System.Windows.Forms;
using ETABSv1;
using EtabsGeometryExport.Model.Service;
using EtabsGeometryExport.Views;

namespace EtabsGeometryExport
{
    public class cPlugin : cPluginContract
    {
        public void Main(ref cSapModel SapModel, ref cPluginCallback IShowCallback)
        {
            try
            {
                // Validate SapModel
                if (SapModel == null)
                {
                    ShowError("SapModel is null - ETABS model not properly loaded", IShowCallback);
                    return;
                }

                try
                {
                    int numFrames = 0;
                    string[] frameNames = new string[0];
                    int ret1 = SapModel.FrameObj.GetNameList(ref numFrames, ref frameNames);

                    int numAreas = 0;
                    string[] areaNames = new string[0];
                    int ret2 = SapModel.AreaObj.GetNameList(ref numAreas, ref areaNames);

                    if ((ret1 != 0 && ret2 != 0) || (numFrames == 0 && numAreas == 0))
                    {
                        ShowError("Model appears to have no structural elements to export", IShowCallback);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"Cannot read model elements: {ex.Message}", IShowCallback);
                    return;
                }

                // Create services
                var etabsService = new ETABSService(SapModel);
                var fileService = new FileService();

                // Create and show Window with services
                var mainWindow = MainWindow.CreateWithServices(etabsService, fileService);

                // Show window as dialog (on same thread)
                mainWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                ShowError($"Unexpected error: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}", IShowCallback);
            }

        }

        public int Info(ref string Info)
        {
            try
            {
                Info = "ETABS Geometry Export Plugin - Opens export window to extract beams, columns, slabs, and walls to JSON format";
                return 0;
            }
            catch (Exception ex)
            {
                Info = $"Plugin info error: {ex.Message}";
                return -1;
            }
        }

        private void ShowError(string message, cPluginCallback callback)
        {
            try
            {
                MessageBox.Show($"Plugin Error:\n\n{message}", "ETABS Export Plugin",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

                callback?.Finish(-1);
            }
            catch
            {
                try
                {
                    callback?.Finish(-1);
                }
                catch { }
            }
        }
    }
}