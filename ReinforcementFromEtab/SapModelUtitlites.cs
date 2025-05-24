using ETABSv1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;

namespace ReinforcementFromEtab
{
    public static class SapModelUtitlites
    {
        public static string[] UnquieNames(this cSapModel SapModel)
        {
            int numOfColumns = 0;
            // retrive all frames unique names in the model include columns and beams
            string[] Names = null;
            int ret = SapModel.FrameObj.GetNameList(ref numOfColumns, ref Names);
       
            return Names ?? Array.Empty<string>();
        }
    }
}
