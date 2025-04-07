using System.IO;
using Newtonsoft.Json;

namespace ExportJsonFileFromRevit
{
    public class JsonDataExporter<T> : IDataExporter<T>
    {
        public void Export(T data, string filePath)
        {
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
    }
}
