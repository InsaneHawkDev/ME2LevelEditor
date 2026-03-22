using System;
using ME2LevelEditor.Services;

namespace ME2LevelEditor.ViewModels
{
    public class AudioViewModel : ViewModelBase, IDisposable
    {
        private readonly AudioService _audioService;
        private bool _isAudioLoaded;
        private bool _isPlaying;
        private double _durationSeconds;
        private double _currentPositionSeconds;
        private float[] _waveformSamples;
        private float _volume = 1f;

        public float Volume
        {
            get => _volume;
            set
            {
                if (SetProperty(ref _volume, value))
                    _audioService.Volume = value;
            }
        }

        public bool IsAudioLoaded
        {
            get => _isAudioLoaded;
            private set => SetProperty(ref _isAudioLoaded, value);
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            private set => SetProperty(ref _isPlaying, value);
        }

        public double DurationSeconds
        {
            get => _durationSeconds;
            private set => SetProperty(ref _durationSeconds, value);
        }

        public double CurrentPositionSeconds
        {
            get => _currentPositionSeconds;
            private set => SetProperty(ref _currentPositionSeconds, value);
        }

        public float[] WaveformSamples
        {
            get => _waveformSamples;
            private set => SetProperty(ref _waveformSamples, value);
        }

        public int AudioSampleCount => WaveformSamples?.Length ?? 0;

        public RelayCommand LoadAudioCommand { get; }
        public RelayCommand PlayPauseCommand { get; }
        public RelayCommand StopCommand { get; }
        public RelayCommand RestartCommand { get; }

        public AudioViewModel()
        {
            _audioService = new AudioService();

            LoadAudioCommand = new RelayCommand(param =>
            {
                if (param is string path)
                {
                    WaveformSamples = _audioService.LoadAudio(path);
                    DurationSeconds = _audioService.Duration.TotalSeconds;
                    IsAudioLoaded = true;
                    OnPropertyChanged(nameof(AudioSampleCount));
                }
            });

            PlayPauseCommand = new RelayCommand(param =>
            {
                if (_audioService.IsPlaying)
                {
                    _audioService.Pause();
                }
                else
                {
                    if (param is double seekSeconds)
                        _audioService.Seek(TimeSpan.FromSeconds(seekSeconds));
                    _audioService.Play();
                }
            }, _ => IsAudioLoaded);

            StopCommand = new RelayCommand(_ =>
            {
                _audioService.Stop();
                IsPlaying = false;
                CurrentPositionSeconds = 0;
            }, _ => IsAudioLoaded);

            RestartCommand = new RelayCommand(_ =>
            {
                _audioService.Seek(TimeSpan.Zero);
                _audioService.Play();
            }, _ => IsAudioLoaded);
        }

        public void SeekTo(double seconds)
        {
            if (!IsAudioLoaded) return;
            _audioService.Seek(TimeSpan.FromSeconds(seconds));
            CurrentPositionSeconds = seconds;
        }

        public void PlayFromPosition(double seconds)
        {
            if (!IsAudioLoaded) return;
            _audioService.Seek(TimeSpan.FromSeconds(seconds));
            _audioService.Play();
        }

        public void UpdatePlaybackPosition()
        {
            if (!IsAudioLoaded) return;
            CurrentPositionSeconds = _audioService.CurrentPosition.TotalSeconds;
            IsPlaying = _audioService.IsPlaying;
        }

        public void Dispose()
        {
            _audioService.Dispose();
        }
    }
}
