using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using ME2LevelEditor.Models;
using ME2LevelEditor.Services;
using Microsoft.Win32;
using ME2LevelEditor.Views;
using L = ME2LevelEditor.Services.LocalizationService;

namespace ME2LevelEditor.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private const double DefaultSamplesPerSecond = 43.0;

        private readonly CacheFileService _cacheFileService;
        private readonly UndoRedoService _undoRedoService;
        private readonly KeyMappingService _keyMappingService;

        public KeyMappingService KeyMappingService => _keyMappingService;

        private HeaderViewModel _header;
        private TimelineViewModel _timeline;
        private SelectionViewModel _selection;
        private AudioViewModel _audio;
        private string _currentFilePath;

        public HeaderViewModel Header
        {
            get => _header;
            private set => SetProperty(ref _header, value);
        }

        public TimelineViewModel Timeline
        {
            get => _timeline;
            private set => SetProperty(ref _timeline, value);
        }

        public SelectionViewModel Selection
        {
            get => _selection;
            private set => SetProperty(ref _selection, value);
        }

        public AudioViewModel Audio
        {
            get => _audio;
            private set => SetProperty(ref _audio, value);
        }

        public ObservableCollection<ObstacleViewModel> Obstacles { get; }
        public ObservableCollection<SectionViewModel> Sections { get; }

        public string CurrentFilePath
        {
            get => _currentFilePath;
            private set
            {
                if (SetProperty(ref _currentFilePath, value))
                {
                    OnPropertyChanged(nameof(WindowTitle));
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        public bool IsDirty => _undoRedoService.IsDirty;

        public double AudioToAnalysisRatio
        {
            get
            {
                if (Timeline.TotalSamples <= 0 || Audio.AudioSampleCount <= 0) return 1.0;
                return (double)Audio.AudioSampleCount / Timeline.TotalSamples;
            }
        }

        public string WindowTitle
        {
            get
            {
                var title = "ME2 Level Editor";
                if (CurrentFilePath != null)
                    title += " - " + Path.GetFileName(CurrentFilePath);
                if (IsDirty)
                    title += " *";
                return title;
            }
        }

        public string StatusText
        {
            get
            {
                var name = CurrentFilePath != null ? Path.GetFileName(CurrentFilePath) : L.T("Msg_NoFile");
                return $"{name} | {L.T("Msg_Obstacles")} {Obstacles.Count} | {L.T("Msg_Sections")} {Sections.Count}";
            }
        }

        public RelayCommand OpenFileCommand { get; }
        public RelayCommand SaveFileCommand { get; }
        public RelayCommand SaveAsFileCommand { get; }
        public RelayCommand ClearAllCommand { get; }
        public RelayCommand UndoCommand { get; }
        public RelayCommand RedoCommand { get; }
        public RelayCommand DeleteSelectedCommand { get; }
        public RelayCommand SelectAllCommand { get; }
        public RelayCommand LoadAudioCommand { get; }
        public RelayCommand ZoomToFitCommand { get; }
        public RelayCommand PlayFromPlayheadCommand { get; }
        public RelayCommand RestartPlaybackCommand { get; }

        public MainViewModel()
        {
            _cacheFileService = new CacheFileService();
            _undoRedoService = new UndoRedoService();
            _keyMappingService = new KeyMappingService();

            Obstacles = new ObservableCollection<ObstacleViewModel>();
            Sections = new ObservableCollection<SectionViewModel>();

            Header = new HeaderViewModel(new CacheHeader());
            Timeline = new TimelineViewModel();
            Selection = new SelectionViewModel();
            Audio = new AudioViewModel();

            OpenFileCommand = new RelayCommand(() =>
            {
                var dialog = new OpenFileDialog
                {
                    Filter = L.T("Filter_CacheFiles")
                };
                if (dialog.ShowDialog() == true)
                    LoadFile(dialog.FileName);
            });

            SaveFileCommand = new RelayCommand(() =>
            {
                if (CurrentFilePath != null)
                    Save(CurrentFilePath);
                else
                    SaveAs();
            });

            SaveAsFileCommand = new RelayCommand(() => SaveAs());

            ClearAllCommand = new RelayCommand(() =>
            {
                int obsCount = Obstacles.Count;
                int secCount = Sections.Count;
                if (obsCount == 0 && secCount == 0) return;

                var result = ConfirmDialog.Show(
                    Application.Current.MainWindow,
                    L.T("Msg_ClearAllConfirm", obsCount, secCount),
                    L.T("Msg_ClearAllTitle"));

                if (result != ConfirmResult.Yes) return;

                Selection.Clear();
                Obstacles.Clear();
                Sections.Clear();
                _undoRedoService.Clear();
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(IsDirty));
                OnPropertyChanged(nameof(WindowTitle));
            }, () => CurrentFilePath != null);

            UndoCommand = new RelayCommand(
                () => { _undoRedoService.Undo(); RefreshAfterUndoRedo(); },
                () => _undoRedoService.CanUndo);

            RedoCommand = new RelayCommand(
                () => { _undoRedoService.Redo(); RefreshAfterUndoRedo(); },
                () => _undoRedoService.CanRedo);

            DeleteSelectedCommand = new RelayCommand(() =>
            {
                var commands = new List<IEditCommand>();

                foreach (var obs in Selection.SelectedObstacles.ToList())
                    commands.Add(new DeleteObstacleCommand(obs, Obstacles));

                foreach (var sec in Selection.SelectedSections.ToList())
                    commands.Add(new DeleteSectionCommand(sec, Sections));

                if (commands.Count > 0)
                {
                    Selection.Clear();
                    var composite = new CompositeCommand("Delete selection", commands);
                    ExecuteCommand(composite);
                }
            }, () => Selection.SelectionCount > 0);

            SelectAllCommand = new RelayCommand(() =>
            {
                Selection.SelectRange(Obstacles);
            });

            ZoomToFitCommand = new RelayCommand(() => Timeline.ZoomToFit());

            PlayFromPlayheadCommand = new RelayCommand(() =>
            {
                if (Audio.IsPlaying)
                {
                    Audio.PlayPauseCommand.Execute(null);
                }
                else
                {
                    double seconds = Timeline.SampleToSeconds(Timeline.PlayheadSample);
                    Audio.PlayPauseCommand.Execute(seconds);
                }
            }, () => Audio.IsAudioLoaded);

            RestartPlaybackCommand = new RelayCommand(() =>
            {
                Audio.RestartCommand.Execute(null);
            }, () => Audio.IsAudioLoaded);

            LoadAudioCommand = new RelayCommand(() =>
            {
                var dialog = new OpenFileDialog
                {
                    Filter = L.T("Filter_AudioFiles")
                };
                if (dialog.ShowDialog() == true)
                {
                    Audio.LoadAudioCommand.Execute(dialog.FileName);
                    if (Audio.IsAudioLoaded)
                    {
                        Timeline.DurationSeconds = Audio.DurationSeconds;
                        OnPropertyChanged(nameof(AudioToAnalysisRatio));
                    }
                }
            });
        }

        public void LoadFile(string path)
        {
            if (!PromptSaveIfDirty()) return;

            try
            {
                var cache = _cacheFileService.LoadFromFile(path);
                LoadCache(cache);
                CurrentFilePath = path;
                _undoRedoService.Clear();
                _undoRedoService.MarkSavePoint();
                OnPropertyChanged(nameof(IsDirty));
                OnPropertyChanged(nameof(WindowTitle));
            }
            catch (Exception ex)
            {
                ConfirmDialog.Show(
                    Application.Current.MainWindow,
                    $"{L.T("Msg_LoadError")}\n\n{ex.Message}",
                    "ME2 Level Editor");
            }
        }

        public void LoadCache(LevelCache cache)
        {
            Header = new HeaderViewModel(cache.Header);

            Obstacles.Clear();
            foreach (var obstacle in cache.Obstacles)
                Obstacles.Add(new ObstacleViewModel(obstacle));

            Sections.Clear();
            foreach (var section in cache.Sections)
                Sections.Add(new SectionViewModel(section));

            Timeline.TotalSamples = cache.Header.TotalSamples;
            if (Audio.IsAudioLoaded)
                Timeline.DurationSeconds = Audio.DurationSeconds;
            else if (cache.Header.TotalSamples > 0)
                Timeline.DurationSeconds = cache.Header.TotalSamples / DefaultSamplesPerSecond;

            Timeline.ZoomToFit();
            Selection.Clear();
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(AudioToAnalysisRatio));
        }

        public LevelCache BuildCacheFromViewModels()
        {
            var cache = new LevelCache();
            Header.UpdateModel();
            cache.Header = Header.Model;

            cache.Obstacles = Obstacles.Select(o => o.Model).ToList();
            cache.Sections = Sections.Select(s => s.Model).ToList();

            return cache;
        }

        public void Save(string path)
        {
            try
            {
                var cache = BuildCacheFromViewModels();
                _cacheFileService.SaveToFile(path, cache);
                CurrentFilePath = path;
                _undoRedoService.MarkSavePoint();
                OnPropertyChanged(nameof(WindowTitle));
                OnPropertyChanged(nameof(IsDirty));
            }
            catch (Exception ex)
            {
                ConfirmDialog.Show(
                    Application.Current.MainWindow,
                    $"{L.T("Msg_SaveError")}\n\n{ex.Message}",
                    "ME2 Level Editor");
            }
        }

        private bool SaveAs()
        {
            var dialog = new SaveFileDialog
            {
                Filter = L.T("Filter_CacheFiles")
            };
            if (dialog.ShowDialog() == true)
            {
                Save(dialog.FileName);
                return true;
            }
            return false;
        }

        public bool PromptSaveIfDirty()
        {
            if (!IsDirty) return true;

            var result = ConfirmDialog.Show(
                Application.Current.MainWindow,
                L.T("Msg_SaveChanges"),
                "ME2 Level Editor",
                showCancel: true);

            switch (result)
            {
                case ConfirmResult.Yes:
                    if (CurrentFilePath != null)
                    {
                        Save(CurrentFilePath);
                        return true;
                    }
                    return SaveAs();
                case ConfirmResult.No:
                    return true;
                default:
                    return false;
            }
        }

        public void ExecuteCommand(IEditCommand cmd)
        {
            _undoRedoService.Execute(cmd);
            OnPropertyChanged(nameof(IsDirty));
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(StatusText));
        }

        public void RefreshAfterUndoRedo()
        {
            OnPropertyChanged(nameof(IsDirty));
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(StatusText));
        }

        public void NotifyStatusChanged()
        {
            OnPropertyChanged(nameof(StatusText));
        }
    }
}
