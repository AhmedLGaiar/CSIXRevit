using Newtonsoft.Json;
using StructLink_X._0.ViewModels;

namespace StructLink_X._0.Models
{
    public class ColumnRCData : ViewModelBase
    {
        private double _width;
        private double _depth;
        private string _sectionName;
        private bool _isRectangular;
        private bool _requiresDesign;
        private double _concreteCover;
        private int _mainBarDiameter;
        private int _numBarsDir3;
        private int _numBarsDir2;
        private int _tieBarDiameter;
        private double _tieSpacing;
        private int _numTiesDir2;
        private int _numTiesDir3;
        private int _sectionCount;
        private double _height;
        private string _uniqueName;
        private string _elementType;

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

        [JsonProperty("SectionName")]
        public string SectionName
        {
            get => _sectionName;
            set { _sectionName = value; OnPropertyChanged(); }
        }

        [JsonProperty("IsRectangle")]
        public bool IsRectangular
        {
            get => _isRectangular;
            set { _isRectangular = value; OnPropertyChanged(); }
        }

        [JsonProperty("ToBeDesign")]
        public bool RequiresDesign
        {
            get => _requiresDesign;
            set { _requiresDesign = value; OnPropertyChanged(); }
        }

        [JsonProperty("Cover")]
        public double ConcreteCover
        {
            get => _concreteCover;
            set { _concreteCover = value; OnPropertyChanged(); }
        }

        [JsonProperty("RebarSize")]
        public int MainBarDiameter
        {
            get => _mainBarDiameter;
            set { _mainBarDiameter = value; OnPropertyChanged(); }
        }

        [JsonProperty("NumberR3Bars")]
        public int NumBarsDir3
        {
            get => _numBarsDir3;
            set { _numBarsDir3 = value; OnPropertyChanged(); }
        }

        [JsonProperty("NumberR2Bars")]
        public int NumBarsDir2
        {
            get => _numBarsDir2;
            set { _numBarsDir2 = value; OnPropertyChanged(); }
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

        [JsonProperty("Number2DirTieBars")]
        public int NumTiesDir2
        {
            get => _numTiesDir2;
            set { _numTiesDir2 = value; OnPropertyChanged(); }
        }

        [JsonProperty("Number3DirTieBars")]
        public int NumTiesDir3
        {
            get => _numTiesDir3;
            set { _numTiesDir3 = value; OnPropertyChanged(); }
        }

        [JsonProperty("SectionCount")]
        public int SectionCount
        {
            get => _sectionCount;
            set { _sectionCount = value; OnPropertyChanged(); }
        }

        [JsonProperty("Height")]
        public double Height
        {
            get => _height;
            set { _height = value; OnPropertyChanged(); }
        }

        [JsonProperty("uniqueName")]
        public string UniqueName
        {
            get => _uniqueName;
            set { _uniqueName = value; OnPropertyChanged(); }
        }

        [JsonProperty("ElementType")]
        public string ElementType
        {
            get => _elementType ?? "Column";
            set { _elementType = value; OnPropertyChanged(); }
        }
    }
}