namespace ReinforcementFromEtab
{
    public class ACIBarLayout
    {
        public int BarsPerR2Face;
        public int BarsPerR3Face;
        public int TotalBars => 2 * BarsPerR2Face + 2 * BarsPerR3Face - 4;
    }
}
