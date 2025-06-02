using ElementsData.Steel;
using ETABSv1;

namespace ReinforcementFromEtab
{
    public class BeamSteel
    {
        public static BeamRCData GetRCBeam(cSapModel SapModel, string SectionName, double maxareasteel
                                                             , string frameNames, double Transverse)

        {
            int ret;
            #region ParameterForBeamRebar
            string MatPropLing = null;
            string MatPropConfine = null;
            double CoverTop = 0;
            double CoverBot = 0;
            double TopLeftArea = 0;
            double TopRightArea = 0;
            double BotLeftArea = 0;
            double BotRightArea = 0;
            #endregion
            #region GetSectionData
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

            ret = SapModel.PropFrame.GetRebarBeam(SectionName,
                ref MatPropLing,
                ref MatPropConfine,
                ref CoverTop,
                ref CoverBot,
                ref TopLeftArea,
                ref TopRightArea,
                ref BotLeftArea,
                ref BotRightArea
            );
            double TransArea = Transverse*1000;
            double barArea = Math.PI * 16 * 16 / 4.0;
            double areaPerBar = Math.PI * Math.Pow(10, 2) / 4.0;
            double areaPerStirrup = 4 * areaPerBar;
            double stirrupsPerMeter = Math.Ceiling(TransArea / areaPerStirrup);


            #endregion
            return new BeamRCData
            {   
                Width = width,
                Depth = depth,
                SectionName = SectionName,
                uniqueName = frameNames,
                Cover = CoverTop,
                RebarSize = 16,

                BotBars = Math.Max(2, (int)Math.Ceiling(maxareasteel / barArea)),
                TopBars = Math.Max(2, (int)Math.Ceiling(maxareasteel / barArea)),

                NumOfLegs=4,
                TieSize = 10,
                TieSpacingLongit = 1000.0 / stirrupsPerMeter
            };
        }
    }
}
