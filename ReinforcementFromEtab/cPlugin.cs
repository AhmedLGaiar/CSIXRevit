using ElementsData;
using ETABSv1;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace ReinforcementFromEtab
{
    public class cPlugin : cPluginContract
    {
        public void Main(ref cSapModel SapModel, ref cPluginCallback ISapPlugin)
        {
            // Get data
            List<ColumnRCData> columns = ReinforcementOfConcreteColumns.GetConcreteColumns(SapModel);

            // Show save file dialog
            var saveDialog = new SaveFileDialog
            {
                Title = "Save JSON File",
                Filter = "JSON Files (*.json)|*.json",
                DefaultExt = "json",
                FileName = "ToEtabsExport.json"
            };

            // If user cancels or doesn't choose a path
            var result = saveDialog.ShowDialog();
            if (result != DialogResult.OK || string.IsNullOrWhiteSpace(saveDialog.FileName))
                return;

            // Save to selected path
            string json = JsonConvert.SerializeObject(columns, Formatting.Indented);
            File.WriteAllText(saveDialog.FileName, json);

            // Finish plugin
            ISapPlugin.Finish(0);
        }

        public int Info(ref string Text)
        {
            Text = "ToEtabs Plugin From ELGaiar";
            return 0;
        }
    }
}
