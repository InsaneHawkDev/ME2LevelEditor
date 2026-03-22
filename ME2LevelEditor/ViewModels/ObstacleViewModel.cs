using ME2LevelEditor.Models;

namespace ME2LevelEditor.ViewModels
{
    public class ObstacleViewModel : ViewModelBase
    {
        private int _sampleId;
        private ObstacleType _type;
        private int? _heldDuration;
        private bool _isSelected;

        public ObstacleDefinition Model { get; }

        public int SampleId
        {
            get => _sampleId;
            set
            {
                if (SetProperty(ref _sampleId, value))
                    Model.SampleId = value;
            }
        }

        public ObstacleType Type
        {
            get => _type;
            set
            {
                if (SetProperty(ref _type, value))
                {
                    Model.Type = value;
                    OnPropertyChanged(nameof(IsHeld));
                }
            }
        }

        public int? HeldDuration
        {
            get => _heldDuration;
            set
            {
                if (SetProperty(ref _heldDuration, value))
                {
                    Model.HeldDuration = value;
                    OnPropertyChanged(nameof(IsHeld));
                }
            }
        }

        public bool IsHeld => Type == ObstacleType.Solid && HeldDuration.HasValue;

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public ObstacleViewModel(ObstacleDefinition obstacle)
        {
            Model = obstacle;
            _sampleId = obstacle.SampleId;
            _type = obstacle.Type;
            _heldDuration = obstacle.HeldDuration;
        }

        public void SyncFromModel()
        {
            _sampleId = Model.SampleId;
            _type = Model.Type;
            _heldDuration = Model.HeldDuration;
            OnPropertyChanged(nameof(SampleId));
            OnPropertyChanged(nameof(Type));
            OnPropertyChanged(nameof(HeldDuration));
            OnPropertyChanged(nameof(IsHeld));
        }
    }
}
