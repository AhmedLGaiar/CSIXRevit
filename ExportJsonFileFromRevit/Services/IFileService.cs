using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportJsonFileFromRevit.Services
{
    public interface IFileService
    {
        string OpenJsonFileDialog();
        Models.JsonStructure ReadJsonFile(string filePath);
    }
}
