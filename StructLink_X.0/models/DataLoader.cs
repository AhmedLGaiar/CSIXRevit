using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace StructLink_X.Models
{
    public class DataLoader
    {
        public static List<ColumnRCData> LoadColumnsOnly(string json)
        {
            try
            {
                // حوليه لكائن JSON
                JObject jsonObject = JObject.Parse(json);

                // خدي قسم columnRCDatas بس
                string columnsJson = jsonObject["columnRCDatas"].ToString();

                // حولي القسم ده لقايمة من الأعمدة
                return JsonConvert.DeserializeObject<List<ColumnRCData>>(columnsJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading JSON: {ex.Message}");
                return new List<ColumnRCData>(); // رجاع قايمة فاضية لو فشل
            }
        }
        public static List<BeamRCData> LoadBeamsOnly(string json)
        {
            try
            {
                JObject jsonObject = JObject.Parse(json);
                string beamsJson = jsonObject["beamRCDatas"].ToString();
                return JsonConvert.DeserializeObject<List<BeamRCData>>(beamsJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading JSON: {ex.Message}");
                return new List<BeamRCData>();
            }
        }
    }
}