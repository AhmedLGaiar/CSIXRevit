namespace EtabsGeometryExport.Model.Service
{

    /// <summary>
    /// Interface for file service operations
    /// </summary>
    public interface IFileService
    {
        bool SaveWithDialog(ETABSStructuralData data, out string filePath);
        void SaveData(ETABSStructuralData data, string filePath);
    }
}
     