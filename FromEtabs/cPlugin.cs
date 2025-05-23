using ETABSDataExtraction;
using ETABSv1;
using System;
using System.Windows;
using System.Windows.Forms;

namespace FromEtabs
{
    public class cPlugin : cPluginContract
    {

        public void Main(ref cSapModel SapModel, ref cPluginCallback ISapPlugin)
        {
            try
            {
                // Attach to the running ETABS instance
                cHelper myHelper = new Helper();
                cOAPI etabsObject = myHelper.GetObject("CSI.ETABS.API.ETABSObject");

                if (etabsObject == null)
                {
                    Console.WriteLine("ETABS instance not found.");
                    return;
                }

                cSapModel sapModel = etabsObject.SapModel;

                // Show file dialog to select JSON file location
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "JSON files (*.json)|*.json",
                    Title = "Save ETABS Data As",
                    FileName = "ETABSData.json"
                };

                if (saveFileDialog.ShowDialog() != DialogResult.OK)
                {
                    Console.WriteLine("Export cancelled.");
                    return;
                }

                string outputPath = saveFileDialog.FileName;

                // Create an instance of the data extractor and execute the export
                var extractor = new ETABSDataExtractor(sapModel, outputPath);
                extractor.Execute();

                // Show success message
                //Console.WriteLine($"JSON exported successfully to:\n{outputPath}");
                System.Windows.MessageBox.Show("Import to ETABS completed successfully.", "Success",
               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine(" Error connecting to ETABS: " + ex.Message);
            }
        }


        public int Info(ref string Text)
        {
            Text = "from Etabs Plugin to revit";
            return 0;
        }
    }
}
