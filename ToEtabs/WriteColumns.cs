using System;
using ETABSv17;

namespace ETABSPlugin
{
    public class WriteColumns
    {
        // Entry point for the ETABS plugin
        public void Main(ref cSapModel SapModel, ref cPluginCallback ISapPlugin)
        {
            try
            {
                
                ISapPlugin.Finish(0);
            }
            catch (Exception ex)
            {
                

                // Indicate an error in the plugin execution
                ISapPlugin.Finish(1);
            }
        }

  
    }
}
