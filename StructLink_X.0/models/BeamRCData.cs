using Newtonsoft.Json;
using StructLink_X._0.ViewModels;

namespace StructLink_X._0.Models
{
    public class BeamRCData : ViewModelBase
    {
        private string _sectionName;
        private string _uniqueName;
        private double _width;
        private double _depth;
        private double _concreteCover;
        private int _numOfLegs;
        private int _mainBarDiameter;
        private int _bottomBars;
        private int _topBars;
        private int _tieBarDiameter;
        private double _tieSpacing;
        private double _length;
        private string _elementType; 

        [JsonProperty("SectionName")]
        public string SectionName
        {
            get => _sectionName;
            set { _sectionName = value; OnPropertyChanged(); }
        }

        [JsonProperty("uniqueName")]
        public string UniqueName
        {
            get => _uniqueName;
            set { _uniqueName = value; OnPropertyChanged(); }
        }

        [JsonProperty("Width")]
        public double Width
        {
            get => _width;
            set { _width = value; OnPropertyChanged(); }
        }

        [JsonProperty("Depth")]
        public double Depth
        {
            get => _depth;
            set { _depth = value; OnPropertyChanged(); }
        }

        [JsonProperty("Cover")]
        public double ConcreteCover
        {
            get => _concreteCover;
            set { _concreteCover = value; OnPropertyChanged(); }
        }

        [JsonProperty("NumOfLegs")]
        public int NumOfLegs
        {
            get => _numOfLegs;
            set { _numOfLegs = value; OnPropertyChanged(); }
        }

        [JsonProperty("RebarSize")]
        public int MainBarDiameter
        {
            get => _mainBarDiameter;
            set { _mainBarDiameter = value; OnPropertyChanged(); }
        }

        [JsonProperty("BotBars")]
        public int BottomBars
        {
            get => _bottomBars;
            set { _bottomBars = value; OnPropertyChanged(); }
        }

        [JsonProperty("TopBars")]
        public int TopBars
        {
            get => _topBars;
            set { _topBars = value; OnPropertyChanged(); }
        }

        [JsonProperty("TieSize")]
        public int TieBarDiameter
        {
            get => _tieBarDiameter;
            set { _tieBarDiameter = value; OnPropertyChanged(); }
        }

        [JsonProperty("TieSpacingLongit")]
        public double TieSpacing
        {
            get => _tieSpacing;
            set { _tieSpacing = value; OnPropertyChanged(); }
        }

        [JsonProperty("Length")]
        public double Length
        {
            get => _length;
            set { _length = value; OnPropertyChanged(); }
        }

        [JsonProperty("ElementType")]
        public string ElementType
        {
            get => _elementType ?? "Beam";
            set { _elementType = value; OnPropertyChanged(); }
        }
    }
}