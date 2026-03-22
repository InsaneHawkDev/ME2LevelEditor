using System;

namespace ME2LevelEditor.ViewModels
{
    public enum ActiveTool
    {
        Pointer,
        PlaceZone,
        PlaceSolid,
        PlaceSolidHeld,
        PlaceSectionLow,
        PlaceSectionNormal,
        PlaceSectionHigh,
        PlaceSectionExtreme
    }

    public class TimelineViewModel : ViewModelBase
    {
        private double _zoomLevel = 1.0;
        private int _scrollOffsetSamples;
        private double _viewportWidthPixels;
        private int _playheadSample;
        private int _totalSamples;
        private double _durationSeconds;
        private ActiveTool _activeTool;
        private bool _autoRevertToPointer;

        public double ZoomLevel
        {
            get => _zoomLevel;
            set
            {
                double clamped = Math.Max(0.1, Math.Min(20.0, value));
                if (SetProperty(ref _zoomLevel, clamped))
                {
                    OnPropertyChanged(nameof(VisibleEndSample));
                    OnPropertyChanged(nameof(ZoomPercent));
                }
            }
        }

        public int ScrollOffsetSamples
        {
            get => _scrollOffsetSamples;
            set
            {
                int clamped = Math.Max(0, Math.Min(TotalSamples, value));
                if (SetProperty(ref _scrollOffsetSamples, clamped))
                {
                    OnPropertyChanged(nameof(VisibleStartSample));
                    OnPropertyChanged(nameof(VisibleEndSample));
                }
            }
        }

        public double ViewportWidthPixels
        {
            get => _viewportWidthPixels;
            set
            {
                if (SetProperty(ref _viewportWidthPixels, value))
                    OnPropertyChanged(nameof(VisibleEndSample));
            }
        }

        public int PlayheadSample
        {
            get => _playheadSample;
            set
            {
                if (SetProperty(ref _playheadSample, value))
                {
                    OnPropertyChanged(nameof(PlayheadTimeText));
                    OnPropertyChanged(nameof(PlayheadSampleText));
                }
            }
        }

        public int TotalSamples
        {
            get => _totalSamples;
            set => SetProperty(ref _totalSamples, value);
        }

        public double DurationSeconds
        {
            get => _durationSeconds;
            set => SetProperty(ref _durationSeconds, value);
        }

        public ActiveTool ActiveTool
        {
            get => _activeTool;
            set => SetProperty(ref _activeTool, value);
        }

        public bool AutoRevertToPointer
        {
            get => _autoRevertToPointer;
            set => SetProperty(ref _autoRevertToPointer, value);
        }

        public void RevertToolIfNeeded()
        {
            if (_autoRevertToPointer && _activeTool != ActiveTool.Pointer)
                ActiveTool = ActiveTool.Pointer;
        }

        public int VisibleStartSample => ScrollOffsetSamples;

        public int VisibleEndSample => ScrollOffsetSamples + (int)(ViewportWidthPixels / ZoomLevel);

        public double ZoomPercent => ZoomLevel * 100;

        public string PlayheadTimeText => FormatTime(SampleToSeconds(PlayheadSample));

        public string PlayheadSampleText => PlayheadSample.ToString();

        public RelayCommand SetToolCommand { get; }

        public TimelineViewModel()
        {
            SetToolCommand = new RelayCommand(param =>
            {
                if (param is ActiveTool tool)
                    ActiveTool = tool;
            });
        }

        public double SampleToPixelX(int sampleId)
        {
            return (sampleId - ScrollOffsetSamples) * ZoomLevel;
        }

        public int PixelXToSample(double pixelX)
        {
            if (TotalSamples <= 0) return 0;
            int sample = (int)(pixelX / ZoomLevel) + ScrollOffsetSamples;
            return Math.Max(0, Math.Min(TotalSamples - 1, sample));
        }

        public double SampleToSeconds(int sampleId)
        {
            if (TotalSamples <= 0) return 0;
            return (double)sampleId / TotalSamples * DurationSeconds;
        }

        public string FormatTime(double seconds)
        {
            int minutes = (int)(seconds / 60);
            double secs = seconds - minutes * 60;
            return $"{minutes:D2}:{secs:00.0}";
        }

        public void ZoomAtPoint(double pixelX, double delta)
        {
            int sampleUnderCursor = PixelXToSample(pixelX);

            double factor = delta > 0 ? 1.15 : 1.0 / 1.15;
            _zoomLevel = Math.Max(0.1, Math.Min(20.0, _zoomLevel * factor));

            _scrollOffsetSamples = sampleUnderCursor - (int)(pixelX / _zoomLevel);
            int maxScroll = Math.Max(0, TotalSamples - (int)(ViewportWidthPixels / _zoomLevel));
            _scrollOffsetSamples = Math.Max(0, Math.Min(maxScroll, _scrollOffsetSamples));

            OnPropertyChanged(nameof(ZoomLevel));
            OnPropertyChanged(nameof(ZoomPercent));
            OnPropertyChanged(nameof(ScrollOffsetSamples));
            OnPropertyChanged(nameof(VisibleStartSample));
            OnPropertyChanged(nameof(VisibleEndSample));
        }

        public void ScrollBy(double deltaPixels)
        {
            int visibleSamples = (int)(ViewportWidthPixels / ZoomLevel);
            int maxScroll = Math.Max(0, TotalSamples - visibleSamples);
            int newOffset = ScrollOffsetSamples + (int)(deltaPixels / ZoomLevel);
            ScrollOffsetSamples = Math.Max(0, Math.Min(maxScroll, newOffset));
        }

        public void ZoomToFit()
        {
            if (TotalSamples <= 0 || ViewportWidthPixels <= 0) return;

            _zoomLevel = Math.Max(0.1, Math.Min(20.0, ViewportWidthPixels / TotalSamples));
            _scrollOffsetSamples = 0;

            OnPropertyChanged(nameof(ZoomLevel));
            OnPropertyChanged(nameof(ZoomPercent));
            OnPropertyChanged(nameof(ScrollOffsetSamples));
            OnPropertyChanged(nameof(VisibleStartSample));
            OnPropertyChanged(nameof(VisibleEndSample));
        }
    }
}
