using ElementsData;
using ETABSv1;
using ReinforcementFromEtab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReinforcementFromEtab
{
    internal class ReinforcementOfConcreteColumns
    {
        public static List<ColumnRCData> GetConcreteColumns(cSapModel SapModel)
        {
            HashSet<string> addedSections = new HashSet<string>();
            List<ColumnRCData> columns = new();
            int ret;
            int numOfColumns = 0;
            // retrive all frames unique names in the model include columns and beams
            string[] Names = null;
            ret = SapModel.FrameObj.GetNameList(ref numOfColumns, ref Names);

            ret = SapModel.Analyze.RunAnalysis();
            ret = SapModel.DesignConcrete.StartDesign();

            int numberItems = 0;
            string[] frameNames = null;
            int[] myOption = null;
            double[] location = null;
            string[] pmmCombo = null;
            double[] pmmArea = null;
            double[] pmmRatio = null;
            string[] vMajorCombo = null;
            double[] avMajor = null;
            string[] vMinorCombo = null;
            double[] avMinor = null;
            string[] errorSummary = null;
            string[] warningSummary = null;

            foreach (string coluniqueName in Names)
            {
                string SectionName = null;
                ret = SapModel.DesignConcrete.GetDesignSection(coluniqueName, ref SectionName);

                ret = SapModel.DesignConcrete.GetSummaryResultsColumn(
                                               coluniqueName,
                                               ref numberItems,
                                               ref frameNames,
                                               ref myOption,
                                               ref location,
                                               ref pmmCombo,
                                               ref pmmArea,
                                               ref pmmRatio,
                                               ref vMajorCombo,
                                               ref avMajor,
                                               ref vMinorCombo,
                                               ref avMinor,
                                               ref errorSummary,
                                               ref warningSummary,
                                               eItemType.Objects
                );
                // to be sure it column not beam
                if (coluniqueName != frameNames[0])
                    continue;
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
                #endregion
                // ToBeCheck
                if (myOption[0] == 1)
                {
                    if (addedSections.Add(SectionName)) // returns false if already exists
                    {
                        columns.Add(new ColumnRCData
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

                        });
                    }
                }
                // ToBeDesign not working yet
                if (myOption[0] == 2)
                {
                    if (addedSections.Add(SectionName)) // returns false if already exists
                    {
                        int NumberOfRebars = (int)Math.Ceiling(pmmArea.Max() / 16);
                        string fileName = null;
                        string matrialProp = null;
                        double T3 = 0;
                        double T2 = 0;
                        int color = 0;
                        string notes = null;
                        string Guid = null;
                        SapModel.PropFrame.GetRectangle(SectionName
                                                     , ref fileName
                                                     , ref matrialProp
                                                     , ref T3
                                                     , ref T2
                                                     , ref color
                                                     , ref notes
                                                     , ref Guid);
                        ACIBarLayout dis = ACIBarDistributor.DistributeBars(NumberOfRebars
                            , T3, T2);
                        columns.Add(new ColumnRCData
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

                        });
                    }
                }
            }
            return columns;
        }
    }
}