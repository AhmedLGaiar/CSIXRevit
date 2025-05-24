//using ElementsData;
//using ETABSv1;

//namespace ReinforcementFromEtab
//{
//    public class BeamSteel
//    {
//        public static BeamRCData GetRCBeam(cSapModel SapModel, string SectionName)
                                                            
//        {
//            int ret;
//            #region ParameterForBeamRebar
//            string MatPropLing = null;
//            string MatPropConfine = null;
//            double CoverTop = 0;
//            double CoverBot = 0;
//            double TopLeftArea = 0;
//            double TopRightArea = 0;
//            double BotLeftArea = 0;
//            double BotRightArea = 0;
//            #endregion
//            #region GetSectionData
//            ret = SapModel.PropFrame.GetRebarBeam(
//                SectionName,
//                ref MatPropLing,
//                ref MatPropConfine,
//                ref CoverTop,
//                ref CoverBot,
//                ref TopLeftArea,
//                ref TopRightArea,
//                ref BotLeftArea,
//                ref BotRightArea
//            );
//            #endregion
//            return new BeamRCData
//            {
//                SectionName = SectionName,
//                Cover = CoverTop, 
//                RebarSize = int.Parse(TopCombo[0]), // Assuming the first combo is the rebar size
//                BotBars = TopArea.Length > 0 ? TopArea[0] > 0 ? (int)(TopArea[0] / 16) : 0 : 0, // Assuming a bar diameter of 16mm
//                TopBars = BotArea.Length > 0 ? BotArea[0] > 0 ? (int)(BotArea[0] / 16) : 0 : 0, // Assuming a bar diameter of 16mm
//                TieSize = int.Parse(TLCombo[0]), // Assuming the first combo is the tie size
//                TieSpacingLongit = TLArea.Length > 0 ? TLArea[0] : 200 // Default spacing if not available
//            };
//        }
//    }
//}
