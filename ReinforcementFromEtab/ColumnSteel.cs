using ElementsData;
using ETABSv1;

namespace ReinforcementFromEtab
{
    public class ColumnSteel
    {
        public static ColumnRCData GetRCColumns(cSapModel SapModel, string SectionName,
                                                          HashSet<string> addedSections, int[] myOption
                                                                     , double[] pmmArea)
        {
            int ret;
            int count = 0;
            #region GetSectionData
            string MatPropLong = null;
            string MatPropConfine = null;
            int Pattern = 0;
            int ConfineType = 0;
            double Cover = 0;
            int NumberCBars = 0;
            // vertical RC
            int NumberR3Bars = 0;
            int NumberR2Bars = 0;
            string RebarSize = null;
            string TieSize = null;
            double TieSpacingLongit = 0;
            int Number2DirTieBars = 0;
            int Number3DirTieBars = 0;
            bool ToBeDesigned = false;

            ret = SapModel.PropFrame.GetRebarColumn(
               SectionName,
               ref MatPropLong,
               ref MatPropConfine,
               ref Pattern,
               ref ConfineType,
               ref Cover,
               ref NumberCBars,
               ref NumberR3Bars,
               ref NumberR2Bars,
               ref RebarSize,
               ref TieSize,
               ref TieSpacingLongit,
               ref Number2DirTieBars,
               ref Number3DirTieBars,
               ref ToBeDesigned
           );
            string FileName = null;
            string MatProp = null;
            double width = 0;
            double depth = 0;
            int Color = 0;
            string Notes = null;
            string GUID = null;
            ret = SapModel.PropFrame.GetRectangle(SectionName, ref FileName
                                     , ref MatProp, ref depth, ref width,
                                     ref Color, ref Notes, ref GUID);
            #endregion
            // ToBeCheck
            if (myOption[0] == 1)
            {

                if (addedSections.Add(SectionName)) // returns false if already exists
                {
                    return new ColumnRCData
                    {
                        SectionName = SectionName,
                        IsRectangle = (ConfineType == 1),
                        ToBeDesign = ToBeDesigned,
                        Cover = Cover,
                        RebarSize = int.Parse(RebarSize),
                        NumberR3Bars = NumberR3Bars,
                        NumberR2Bars = NumberR2Bars,
                        TieSize = int.Parse(TieSize),
                        TieSpacingLongit = TieSpacingLongit,
                        Number2DirTieBars = Number2DirTieBars,
                        Number3DirTieBars = Number3DirTieBars,
                        Width = width,
                        Depth = depth,
                        SectionCount = CountColumnsWithSection(SapModel, SectionName)
                    };
                }
                return null; // if already exists, return null
            }
            // ToBeDesign not working yet
            else if (myOption[0] == 2)
            {
                if (addedSections.Add(SectionName)) // returns false if already exists
                {
                    double barArea = Math.PI * 16 * 16 / 4.0;

                    int NumberOfRebars = Math.Max((int)Math.Ceiling(pmmArea.Max() / barArea), 4);

                    ACIBarLayout dis = ACIBarDistributor.DistributeBars(NumberOfRebars
                        , depth, width);
                    return new ColumnRCData
                    {
                        SectionName = SectionName,
                        IsRectangle = (ConfineType == 1),
                        ToBeDesign = ToBeDesigned,
                        Cover = Cover,
                        RebarSize = int.Parse(RebarSize),
                        NumberR3Bars = dis.BarsPerR3Face,
                        NumberR2Bars = dis.BarsPerR2Face,
                        TieSize = int.Parse(TieSize),
                        TieSpacingLongit = TieSpacingLongit,
                        Number2DirTieBars = Number2DirTieBars,
                        Number3DirTieBars = Number3DirTieBars,
                        Width = width,
                        Depth = depth
                    };
                }
            }

            return null;

        }
        public static int CountColumnsWithSection(cSapModel SapModel, string targetSectionName)
        {
            int count = 0;
            int numberItems = 0;
            string[] frameNames = null;

            // Get all frame object names
            int ret = SapModel.FrameObj.GetNameList(ref numberItems, ref frameNames);
            if (ret != 0 || frameNames == null) return 0;

            for (int i = 0; i < numberItems; i++)
            {
                string name = frameNames[i];

                // Get section name
                string sectionName = null;
                string SAuto = null;
                ret = SapModel.FrameObj.GetSection(name, ref sectionName, ref SAuto);
                if (ret != 0) continue;

                if (sectionName != targetSectionName)
                    continue;

                // Get points of the frame
                string point1 = null, point2 = null;
                ret = SapModel.FrameObj.GetPoints(name, ref point1, ref point2);
                if (ret != 0) continue;

                double x1 = 0, y1 = 0, z1 = 0;
                double x2 = 0, y2 = 0, z2 = 0;

                // Get coordinates of both ends
                SapModel.PointObj.GetCoordCartesian(point1, ref x1, ref y1, ref z1);
                SapModel.PointObj.GetCoordCartesian(point2, ref x2, ref y2, ref z2);

                // Check if vertical (difference in Z)
                if (Math.Abs(z2 - z1) > 0.01)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
