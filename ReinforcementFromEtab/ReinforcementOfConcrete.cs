using ElementsData;
using ETABSv1;

namespace ReinforcementFromEtab
{
    internal class ReinforcementOfConcrete
    {
        public static FrameRCData GetRCFrames(cSapModel SapModel)
        {
            HashSet<string> addedSections = new();
            List<ColumnRCData> columns = new();
            List<BeamRCData> beams = new();
            int ret;
            // retrive all frames unique names in the model include columns and beams
            string[] Names = SapModel.UnquieNames();
            ret = SapModel.Analyze.RunAnalysis();
            ret = SapModel.DesignConcrete.StartDesign();

            int[] myOption = null;

            #region CommonParameters
            int numberItems = 0;
            string[] frameNames = null;
            double[] location = null;
            string[] errorSummary = null;
            string[] warningSummary = null;
            string[] vMajorCombo = null;
            #endregion

            #region ColumnParameters
            string[] pmmCombo = null;
            double[] pmmArea = null;
            double[] pmmRatio = null;
            double[] avMajor = null;
            string[] vMinorCombo = null;
            double[] avMinor = null;
            #endregion

            #region BeamParameters
            string[] TopCombo = null;
            double[] TopArea = null;
            string[] BotCombo = null;
            double[] BotArea = null;
            double[] vMajorArea = null;
            string[] TLCombo = null;
            double[] TLArea = null;
            string[] TTCombo = null;
            double[] TTArea = null;
            #endregion

            foreach (string uniqueName in Names)
            {
                string SectionName = null;
                ret = SapModel.DesignConcrete.GetDesignSection(uniqueName, ref SectionName);
                if (SectionName != null)
                {
                    ret = SapModel.DesignConcrete.GetSummaryResultsColumn(
                                                   uniqueName,
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
                    if (uniqueName == frameNames[0])
                    {
                        var column = ColumnSteel.GetRCColumns(SapModel, SectionName, addedSections, myOption, pmmArea);
                        if (column != null)
                            columns.Add(column);
                    }
                    else
                    {
                        ret = SapModel.DesignConcrete.GetSummaryResultsBeam(uniqueName,
                                                                            ref numberItems,
                                                                            ref frameNames,
                                                                            ref location,
                                                                            ref TopCombo,
                                                                            ref TopArea,
                                                                            ref BotCombo,
                                                                            ref BotArea,
                                                                            ref vMajorCombo,
                                                                            ref vMajorArea,
                                                                            ref TLCombo,
                                                                            ref TLArea,
                                                                            ref TTCombo,
                                                                            ref TTArea,
                                                                            ref errorSummary,
                                                                            ref warningSummary,
                                                                            eItemType.Objects
                        );
                        beams.Add(BeamSteel.GetRCBeam(SapModel, SectionName,
                                      TopArea.Concat(BotArea).Max()
                                    , frameNames.First(), vMajorArea.Max()+TTArea.Max()*2));
                    }
                }
            }
            return new FrameRCData
            {
                columnRCDatas = columns,
                beamRCDatas = beams
            };
        }
    }
}
