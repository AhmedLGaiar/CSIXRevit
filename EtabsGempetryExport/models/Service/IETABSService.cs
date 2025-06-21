using System.Collections.Generic;
using System.Threading.Tasks;
using ElementsData;
using ElementsData.Geometry;

namespace EtabsGempetryExport.Model.Service
{


    /// <summary>
    /// Interface for ETABS service operations
    /// </summary>
    public interface IETABSService
    {
        bool ConnectToETABS();
        Task<List<BeamGeometryData>> ExtractBeamsAsync();
        Task<List<ColumnGeometryData>> ExtractColumnsAsync();
        Task<List<SlabData>> ExtractSlapAsync();
        Task<List<StructuralWallData>> ExtractStructuralWallAsync();
        Task<ETABSStructuralData> ExtractAllDataAsync();

        void Dispose();
    }
}
   
