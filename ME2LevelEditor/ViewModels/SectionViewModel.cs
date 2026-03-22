using ME2LevelEditor.Models;

namespace ME2LevelEditor.ViewModels
{
    public class SectionViewModel : ViewModelBase
    {
        private IntensityLevel _intensity;
        private int _startSample;
        private int _length;
        private bool _isAngelJump;
        private bool _isSelected;

        public IntensitySection Model { get; }

        public IntensityLevel Intensity
        {
            get => _intensity;
            set
            {
                if (SetProperty(ref _intensity, value))
                    Model.Intensity = value;
            }
        }

        public int StartSample
        {
            get => _startSample;
            set
            {
                if (SetProperty(ref _startSample, value))
                {
                    Model.StartSample = value;
                    OnPropertyChanged(nameof(EndSample));
                }
            }
        }

        public int Length
        {
            get => _length;
            set
            {
                if (SetProperty(ref _length, value))
                {
                    Model.Length = value;
                    OnPropertyChanged(nameof(EndSample));
                }
            }
        }

        public bool IsAngelJump
        {
            get => _isAngelJump;
            set
            {
                if (SetProperty(ref _isAngelJump, value))
                    Model.IsAngelJump = value;
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public int EndSample => StartSample + Length;

        public SectionViewModel(IntensitySection section)
        {
            Model = section;
            _intensity = section.Intensity;
            _startSample = section.StartSample;
            _length = section.Length;
            _isAngelJump = section.IsAngelJump;
        }

        public void SyncFromModel()
        {
            _intensity = Model.Intensity;
            _startSample = Model.StartSample;
            _length = Model.Length;
            _isAngelJump = Model.IsAngelJump;
            OnPropertyChanged(nameof(Intensity));
            OnPropertyChanged(nameof(StartSample));
            OnPropertyChanged(nameof(Length));
            OnPropertyChanged(nameof(IsAngelJump));
            OnPropertyChanged(nameof(EndSample));
        }
    }
}
