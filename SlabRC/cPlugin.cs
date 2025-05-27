using ETABSv1;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace SlabRC
{
    public class SlabShellResult
    {
        public string SlabName { get; set; }
        public string ElementID { get; set; }
        public string Story { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double TopDir1 { get; set; }
        public double TopDir2 { get; set; }
        public double BotDir1 { get; set; }
        public double BotDir2 { get; set; }
    }

    public class cPlugin : cPluginContract
    {
        public void Main(ref cSapModel SapModel, ref cPluginCallback ISapPlugin)
        {
            // Ensure analysis is done and design is started
            SapModel.DesignConcrete.StartDesign();

            // Get all area (shell) objects
            string[] slabNames = Array.Empty<string>();
            int num = 0;
            SapModel.AreaObj.GetNameList(ref num, ref slabNames);

            var allResults = new List<SlabShellResult>();

            foreach (string slab in slabNames)
            {
                int numItems = 0;
                string[] obj = new string[0];
                string[] elm = new string[0];
                string[] sec = new string[0];
                string[] story = new string[0];
                double[] x = new double[0];
                double[] y = new double[0];
                double[] z = new double[0];
                double[] top1 = new double[0];
                double[] top2 = new double[0];
                double[] bot1 = new double[0];
                double[] bot2 = new double[0];

                int res = SapModel.DesignConcreteShell.GetSummaryResultsShell(
                    slab, ref numItems, ref obj, ref elm, ref sec, ref story,
                    ref x, ref y, ref z,
                    ref top1, ref top2, ref bot1, ref bot2);

                for (int i = 0; i < numItems; i++)
                {
                    allResults.Add(new SlabShellResult
                    {
                        SlabName = obj[i],
                        ElementID = elm[i],
                        Story = story[i],
                        X = x[i],
                        Y = y[i],
                        Z = z[i],
                        TopDir1 = top1[i],
                        TopDir2 = top2[i],
                        BotDir1 = bot1[i],
                        BotDir2 = bot2[i],
                    });
                }
            }

            // Export to JSON
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(desktop, "SlabShellReinforcement.json");

            string json = JsonConvert.SerializeObject(allResults, Formatting.Indented);
            File.WriteAllText(filePath, json);

            ISapPlugin.Finish(0);
        }

        public int Info(ref string Text)
        {
            Text = "Slab Shell Reinforcement Export (No Strips)";
            return 0;
        }
    }
}
