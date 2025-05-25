
namespace ElementsData
{
    public class FrameRCData
    {
        public List<ColumnRCData> columnRCDatas { get; set; }
        public List<BeamRCData> beamRCDatas { get; set; }

        public FrameRCData()
        {
            columnRCDatas = new List<ColumnRCData>();
            beamRCDatas = new List<BeamRCData>();
        }
    }
}
