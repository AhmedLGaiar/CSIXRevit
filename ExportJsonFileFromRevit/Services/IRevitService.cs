using Autodesk.Revit.DB;
using ElementsData.Steel;
using ElementsData.Geometry;
using System.Collections.Generic;

namespace ExportJsonFileFromRevit.Services
{
    public interface IRevitService
    {
        // Process both beams and columns into a FrameRCData object
        FrameRCData ProcessFrame(Document doc);

        // Process beams into a List<BeamRCData>
        void ProcessBeams(Document doc, List<BeamRCData> beams);

        // Process columns into a List<ColumnRCData>
        void ProcessColumns(Document doc, List<ColumnRCData> columns);

        // Optional: Support for geometry data if needed
        void ProcessBeamsGeometry(Document doc, List<BeamGeometryData> beams);
        void ProcessColumnsGeometry(Document doc, List<ColumnGeometryData> columns);

        void ProcessWalls(Document doc, List<WallRCData> walls); // New method for walls
    }
}