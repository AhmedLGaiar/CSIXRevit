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
                        Depth = depth
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
    }
}
