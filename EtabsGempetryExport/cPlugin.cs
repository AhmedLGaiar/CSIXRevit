using System;
using System.IO;
using System.Windows.Forms;
using ETABSv1;
using Newtonsoft.Json;
using EtabsGempetryExport.Model;
using EtabsGempetryExport.Model.HelperClasses;
using EtabsGempetryExport.Model.Service;

namespace EtabsGempetryExport.Plugins
{
    public class cPlugin : cPluginContract
    {
        private cOAPI _etabsObject;
        private cSapModel _sapModel;

        public void Main(ref cOAPI EtabsObject, ref bool Cancel)
        {
            try
            {
                MessageBox.Show("Plugin started!", "ETABS Geometry Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _etabsObject = EtabsObject;
                _sapModel = EtabsObject?.SapModel;

                if (_sapModel == null)
                {
                    MessageBox.Show("No valid ETABS model found!", "ETABS Geometry Export",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Cancel = true;
                    return;
                }

                // Initialize ETABS service with SapModel
                IETABSService etabsService = new ETABSService(_sapModel);
                MessageBox.Show("Extracting structural data...");

                // Extract structural data
                var structuralData = etabsService.ExtractAllDataAsync().GetAwaiter().GetResult();

                if (structuralData.TotalElementCount == 0)
                {
                    MessageBox.Show("No structural data found in the model!",
                        "ETABS Geometry Export", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Cancel = true;
                    return;
                }

                // Initialize FileService to save data
                IFileService fileService = new FileService();
                if (fileService.SaveWithDialog(structuralData, out string filePath))
                {
                    MessageBox.Show($"Data extracted successfully!\n\n" +
                                    $"Beams: {structuralData.Beams.Count}\n" +
                                    $"Columns: {structuralData.Columns.Count}\n" +
                                    $"Slabs: {structuralData.Slabs.Count}\n" +
                                    $"Walls: {structuralData.StructWalls.Count}\n" +
                                    $"Saved to: {Path.GetFileName(filePath)}",
                                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Cancel = false;
                }
                else
                {
                    MessageBox.Show("Save operation cancelled.",
                        "ETABS Geometry Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Cancel = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Plugin error: {ex.Message}\nStackTrace: {ex.StackTrace}",
                    "ETABS Geometry Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Cancel = true;
            }
        }

        public void Main(ref cSapModel SapModel, ref cPluginCallback ISapPlugin)
        {
            try
            {
                MessageBox.Show("Plugin started!", "ETABS Geometry Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _sapModel = SapModel;

                if (_sapModel == null)
                {
                    MessageBox.Show("No valid ETABS model provided!",
                        "ETABS Geometry Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ISapPlugin.Finish(1);
                    return;
                }

                // Initialize ETABS service with SapModel
                IETABSService etabsService = new ETABSService(_sapModel);
                MessageBox.Show("Extracting structural data...");

                // Extract structural data
                var structuralData = etabsService.ExtractAllDataAsync().GetAwaiter().GetResult();

                if (structuralData.TotalElementCount == 0)
                {
                    MessageBox.Show("No structural data found in the model!",
                        "ETABS Geometry Export", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    ISapPlugin.Finish(1);
                    return;
                }

                // Initialize FileService to save data
                IFileService fileService = new FileService();
                if (fileService.SaveWithDialog(structuralData, out string filePath))
                {
                    MessageBox.Show($"Data extracted successfully!\n\n" +
                                    $"Beams: {structuralData.Beams.Count}\n" +
                                    $"Columns: {structuralData.Columns.Count}\n" +
                                    $"Slabs: {structuralData.Slabs.Count}\n" +
                                    $"Walls: {structuralData.StructWalls.Count}\n" +
                                    $"Saved to: {Path.GetFileName(filePath)}",
                                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ISapPlugin.Finish(0);
                }
                else
                {
                    ISapPlugin.Finish(1);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Plugin error: {ex.Message}\nStackTrace: {ex.StackTrace}",
                    "ETABS Geometry Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ISapPlugin.Finish(1);
            }
        }

        public int Info(ref string Text)
        {
            Text = "ETABS Geometry Export Plugin\n" +
                   "Extracts structural data (beams, columns, slabs, and walls) from ETABS and saves to JSON.\n" +
                   "Version: 1.0\n" +
                   "Developed by: ITI Team\n" +
                   "Contact: support@example.com";
            return 0;
        }

        public void CleanUp()
        {
            // Release COM objects to prevent memory leaks
            ETABSHelper.ReleaseCOMObjects(_etabsObject, _sapModel);
            _etabsObject = null;
            _sapModel = null;
        }
    }
}