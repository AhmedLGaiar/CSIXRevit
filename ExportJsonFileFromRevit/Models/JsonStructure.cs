using ElementsData;
using ElementsData.Geometry;
using ElementsData.Steel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportJsonFileFromRevit.Models
{
    public class JsonStructure
    {
        [JsonProperty("Beams")]
        public List<BeamRCData> Beams { get; set; }

        [JsonProperty("Columns")]
        public List<ColumnRCData> Columns { get; set; }

        [JsonProperty("Slabs")]
        public List<SlabData> Slabs { get; set; } // Uses ElementsData.Geometry.SlabData
        [JsonProperty("StructWalls")]
        public List<WallRCData> StructWalls { get; set; } // Updated to List<WallRCData>    }
    }
}