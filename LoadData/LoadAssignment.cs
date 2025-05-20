using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadData
{
    public class LoadAssignment
    {
        public string ElementID { get; set; } 
        public string LoadPattern { get; set; }
        public string LoadType { get; set; }
        public double Value { get; set; }
        public string Unit { get; set; }
        public int Dir { get; set; }
        public double? StartDistance { get; set; }
        public double? EndDistance { get; set; }
        public string RelativeDistance { get; set; }

        

    }
}
