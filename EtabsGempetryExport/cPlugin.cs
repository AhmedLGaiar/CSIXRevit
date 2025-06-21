using System;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using ETABSv1;
using Newtonsoft.Json;
using EtabsGempetryExport.Model;
using EtabsGempetryExport.Model.Service;
using EtabsGempetryExport.Views;
using EtabsGempetryExport.ViewModel;

namespace EtabsGempetryExport
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

                //string modelPath = "";
                //int result = SapModel.GetModelFilename(modelPath);
                //if (result != 0)
                //{
                //    ShowError("Cannot access ETABS model. Ensure model is open and unlocked.", IShowCallback);
                //    return;
                //}

                // Check if model has structural elements
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

                // Capture the SapModel reference in a local variable to use in lambda
                cSapModel sapModelLocal = SapModel;

                // Launch WPF Window on STA thread
                Thread staThread = new Thread(() =>
                {
                    try
                    {
                        // Create services using the local variable
                        var etabsService = new ETABSService(sapModelLocal);
                        var fileService = new FileService();

                        // Create and show Window with services
                        var mainWindow = MainWindow.CreateWithServices(etabsService, fileService);

                        // Show window as dialog
                        mainWindow.ShowDialog();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to open export window: {ex.Message}",
                            "Plugin Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                });

                // Set STA apartment state for WPF
                staThread.SetApartmentState(ApartmentState.STA);
                staThread.Start();
                staThread.Join(); // Wait for window to close

                // Signal success after window closes
                IShowCallback.Finish(0);
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