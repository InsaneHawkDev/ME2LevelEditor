using ME2LevelEditor.Models;

namespace ME2LevelEditor.ViewModels
{
    public class HeaderViewModel : ViewModelBase
    {
        private string _version;
        private int _totalSamples;
        private double _displayBPM;
        private int _bpm;
        private bool _loudnessCalculated;
        private double? _loudnessLUFS;

        public CacheHeader Model { get; }

        public string Version
        {
            get => _version;
            set
            {
                if (SetProperty(ref _version, value))
                    OnPropertyChanged(nameof(IsLufsVisible));
            }
        }

        public int TotalSamples
        {
            get => _totalSamples;
            set => SetProperty(ref _totalSamples, value);
        }

        public double DisplayBPM
        {
            get => _displayBPM;
            set => SetProperty(ref _displayBPM, value);
        }

        public int BPM
        {
            get => _bpm;
            set => SetProperty(ref _bpm, value);
        }

        public bool LoudnessCalculated
        {
            get => _loudnessCalculated;
            set => SetProperty(ref _loudnessCalculated, value);
        }

        public double? LoudnessLUFS
        {
            get => _loudnessLUFS;
            set => SetProperty(ref _loudnessLUFS, value);
        }

        public bool IsLufsVisible => Version == "0.8.4";

        public HeaderViewModel(CacheHeader header)
        {
            Model = header;
            _version = header.Version;
            _totalSamples = header.TotalSamples;
            _displayBPM = header.DisplayBPM;
            _bpm = header.BPM;
            _loudnessCalculated = header.LoudnessCalculated;
            _loudnessLUFS = header.LoudnessLUFS;
        }

        public void UpdateModel()
        {
            Model.Version = Version;
            Model.TotalSamples = TotalSamples;
            Model.DisplayBPM = DisplayBPM;
            Model.BPM = BPM;
            Model.LoudnessCalculated = LoudnessCalculated;
            Model.LoudnessLUFS = LoudnessLUFS;
        }
    }
}
