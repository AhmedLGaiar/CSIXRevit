using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadData
{
    public class RootObject
    {
        public List<LoadDefinition> LoadDefinitions { get; set; }
        public List<LoadCombination> LoadCombinations { get; set; }
    }
}
