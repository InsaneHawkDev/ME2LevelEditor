using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ME2LevelEditor.Models;
using ME2LevelEditor.Services;
using ME2LevelEditor.ViewModels;

namespace ME2LevelEditor.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;
        private readonly Dictionary<Key, ObstacleViewModel> _activeHolds = new Dictionary<Key, ObstacleViewModel>();
        private readonly Dictionary<Key, SectionViewModel> _activeSectionHolds = new Dictionary<Key, SectionViewModel>();
        private const int HeldThresholdSamples = 3;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            CompositionTarget.Rendering += OnRendering;
            Closing += OnWindowClosing;
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, (s, e) => Close()));

            PreviewKeyDown += OnPreviewKeyDown;
            PreviewKeyUp += OnPreviewKeyUp;

            BuildLanguageMenu();
        }

        private void BuildLanguageMenu()
        {
            LanguageMenu.Items.Clear();
            foreach (var lang in LocalizationService.AvailableLanguages)
            {
                var item = new MenuItem
                {
                    Header = lang.DisplayName,
                    IsChecked = lang.Code == LocalizationService.CurrentLanguage,
                    Tag = lang.Code
                };
                item.Click += OnLanguageSelected;
                LanguageMenu.Items.Add(item);
            }
        }

        private void OnLanguageSelected(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.Tag is string langCode)
            {
                LocalizationService.SetLanguage(langCode);
                BuildLanguageMenu();
                _viewModel.NotifyStatusChanged();
            }
        }

        private int CurrentPlayheadSample => _viewModel.Timeline.PlayheadSample;

        private bool IsLivePlacementActive => _viewModel.Audio.IsPlaying &&
                                              _viewModel.Audio.DurationSeconds > 0 &&
                                              _viewModel.Timeline.TotalSamples > 0;

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                _viewModel.Selection.Clear();
                _viewModel.Timeline.ActiveTool = ActiveTool.Pointer;
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Home)
            {
                _viewModel.Timeline.ScrollOffsetSamples = 0;
                e.Handled = true;
                return;
            }

            if (e.Key == Key.End)
            {
                _viewModel.Timeline.ScrollOffsetSamples = _viewModel.Timeline.TotalSamples;
                e.Handled = true;
                return;
            }

            if (!IsLivePlacementActive) return;
            if (e.IsRepeat) return;

            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            var action = _viewModel.KeyMappingService.GetAction(key);
            if (action == PlacementAction.None) return;

            int sample = CurrentPlayheadSample;

            switch (action)
            {
                case PlacementAction.PlaceZone:
                    CreateObstacleVm(sample, ObstacleType.Zone, null);
                    e.Handled = true;
                    break;

                case PlacementAction.PlaceSolid:
                    var solidVm = CreateObstacleVm(sample, ObstacleType.Solid, 1);
                    _activeHolds[key] = solidVm;
                    e.Handled = true;
                    break;

                case PlacementAction.PlaceSectionLow:
                case PlacementAction.PlaceSectionNormal:
                case PlacementAction.PlaceSectionHigh:
                case PlacementAction.PlaceSectionExtreme:
                    var sectionVm = BeginSection(sample, ActionToIntensity(action));
                    _activeSectionHolds[key] = sectionVm;
                    e.Handled = true;
                    break;
            }
        }

        private void OnPreviewKeyUp(object sender, KeyEventArgs e)
        {
            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            if (_activeHolds.TryGetValue(key, out var holdVm))
            {
                _activeHolds.Remove(key);
                int duration = CurrentPlayheadSample - holdVm.SampleId;

                if (duration > HeldThresholdSamples)
                    holdVm.HeldDuration = duration;
                else
                    holdVm.HeldDuration = null;

                e.Handled = true;
            }

            if (_activeSectionHolds.TryGetValue(key, out var sectionVm))
            {
                _activeSectionHolds.Remove(key);
                int endSample = CurrentPlayheadSample;
                int length = Math.Max(1, endSample - sectionVm.StartSample);
                sectionVm.Length = length;
                e.Handled = true;
            }
        }

        private ObstacleViewModel CreateObstacleVm(int sampleId, ObstacleType type, int? heldDuration)
        {
            var model = new ObstacleDefinition
            {
                SampleId = sampleId,
                Type = type,
                HeldDuration = heldDuration
            };
            var vm = new ObstacleViewModel(model);
            _viewModel.ExecuteCommand(new AddObstacleCommand(vm, _viewModel.Obstacles));
            return vm;
        }

        private SectionViewModel BeginSection(int startSample, IntensityLevel intensity)
        {
            var model = new IntensitySection
            {
                Intensity = intensity,
                StartSample = startSample,
                Length = 1,
                IsAngelJump = false
            };
            var vm = new SectionViewModel(model);
            _viewModel.ExecuteCommand(new AddSectionCommand(vm, _viewModel.Sections));
            return vm;
        }

        private static IntensityLevel ActionToIntensity(PlacementAction action)
        {
            switch (action)
            {
                case PlacementAction.PlaceSectionLow: return IntensityLevel.Low;
                case PlacementAction.PlaceSectionNormal: return IntensityLevel.Normal;
                case PlacementAction.PlaceSectionHigh: return IntensityLevel.High;
                case PlacementAction.PlaceSectionExtreme: return IntensityLevel.Extreme;
                default: return IntensityLevel.Normal;
            }
        }

        private void OnRendering(object sender, EventArgs e)
        {
            _viewModel.Audio.UpdatePlaybackPosition();
            if (_viewModel.Audio.IsPlaying && _viewModel.Audio.DurationSeconds > 0 && _viewModel.Timeline.TotalSamples > 0)
            {
                _viewModel.Timeline.PlayheadSample = (int)(_viewModel.Audio.CurrentPositionSeconds / _viewModel.Audio.DurationSeconds * _viewModel.Timeline.TotalSamples);
                AutoScrollToPlayhead();
            }

            UpdateActiveHoldLengths();
            UpdateActiveSectionLengths();
        }

        private void UpdateActiveHoldLengths()
        {
            if (_activeHolds.Count == 0) return;
            int current = CurrentPlayheadSample;
            foreach (var kv in _activeHolds)
            {
                int duration = Math.Max(1, current - kv.Value.SampleId);
                kv.Value.HeldDuration = duration;
            }
        }

        private void UpdateActiveSectionLengths()
        {
            if (_activeSectionHolds.Count == 0) return;
            int current = CurrentPlayheadSample;
            foreach (var kv in _activeSectionHolds)
            {
                int length = Math.Max(1, current - kv.Value.StartSample);
                kv.Value.Length = length;
            }
        }

        private void AutoScrollToPlayhead()
        {
            int playhead = _viewModel.Timeline.PlayheadSample;
            int visStart = _viewModel.Timeline.VisibleStartSample;
            int visEnd = _viewModel.Timeline.VisibleEndSample;
            if (playhead < visStart || playhead > visEnd)
            {
                int visibleRange = visEnd - visStart;
                _viewModel.Timeline.ScrollOffsetSamples = Math.Max(0, playhead - visibleRange / 4);
            }
        }

        private void OnOpenKeyMappings(object sender, RoutedEventArgs e)
        {
            var dialog = new KeyMappingDialog(_viewModel.KeyMappingService) { Owner = this };
            dialog.ShowDialog();
        }

        private void OnOpenAbout(object sender, RoutedEventArgs e)
        {
            var dialog = new AboutDialog { Owner = this };
            dialog.ShowDialog();
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            if (!_viewModel.PromptSaveIfDirty())
            {
                e.Cancel = true;
                return;
            }
            CompositionTarget.Rendering -= OnRendering;
            _viewModel.Audio.Dispose();
        }
    }
}
