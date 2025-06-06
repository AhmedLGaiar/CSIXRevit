﻿using System.IO;
using ElementsData.Geometry;
using ETABSv1;
using Newtonsoft.Json;

namespace ToEtabs.Utilities
{
    internal class ColumnUtilities
    {
        public static List<ColumnGeometryData> LoadColumnGeometryData(string jsonPath)
        {
            if (!File.Exists(jsonPath))
                throw new FileNotFoundException($"The file {jsonPath} does not exist.");

            string json = File.ReadAllText(jsonPath);
            List<ColumnGeometryData> columnList = JsonConvert.DeserializeObject<List<ColumnGeometryData>>(json);

            return columnList;
        }

        public static int DefineColumnSection(cSapModel SapModel,string Name,string MatrialProp,double depth,double width)
        {
            SapModel.PropFrame.SetRectangle(Name, MatrialProp, depth, width);
            return 0;
        } 

        public static int DrawColumnByCoordinates(cSapModel SapModel,double x1,double y1,double z1,double x2,double y2,double z2,
            string ColName,string SectionName)
        {
            string DefinedName = null;
            SapModel.FrameObj.AddByCoord(x1, y1, z1, x2, y2, z2,ref DefinedName, SectionName, ColName);
            return 0;
        }

    }
}