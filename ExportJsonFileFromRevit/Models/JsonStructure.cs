using ElementsData.Steel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElementsData;

namespace ExportJsonFileFromRevit.Models
{
    public class JsonStructure
    {
        [JsonProperty("Beams")]
        public List<BeamRCData> Beams { get; set; }

        [JsonProperty("Columns")]
        public List<ColumnRCData> Columns { get; set; }

        [JsonProperty("Slabs")]
        public object Slabs { get; set; }

        [JsonProperty("StructWalls")]
        public object StructWalls { get; set; }
    }
}
