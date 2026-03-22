using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ME2LevelEditor.Models;
using ME2LevelEditor.Services;
using ME2LevelEditor.ViewModels;
using L = ME2LevelEditor.Services.LocalizationService;

namespace ME2LevelEditor.Views
{
    public partial class SectionTrack : UserControl
    {
        private MainViewModel VM => DataContext as MainViewModel;
        private bool _isDragging;
        private bool _isResizing;
        private Point _dragStart;
        private int _dragStartSample;
        private int _originalStartSample;
        private int _originalLength;
        private SectionViewModel _dragSection;
        private bool _isCreating;
        private int _createStartSample;
        private SectionViewModel _hoveredSection;
        private SectionViewModel _playheadHoveredSection;

        public SectionTrack()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is MainViewModel oldVm)
            {
                oldVm.Sections.CollectionChanged -= OnSectionsCollectionChanged;
                oldVm.Timeline.PropertyChanged -= OnTimelinePropertyChanged;
                UnsubscribeSections(oldVm.Sections);
            }

            if (e.NewValue is MainViewModel newVm)
            {
                newVm.Sections.CollectionChanged += OnSectionsCollectionChanged;
                newVm.Timeline.PropertyChanged += OnTimelinePropertyChanged;
                SubscribeSections(newVm.Sections);
                RedrawSections();
            }
        }

        private void SubscribeSections(IEnumerable<SectionViewModel> sections)
        {
            foreach (var s in sections)
                s.PropertyChanged += OnSectionPropertyChanged;
        }

        private void UnsubscribeSections(IEnumerable<SectionViewModel> sections)
        {
            foreach (var s in sections)
                s.PropertyChanged -= OnSectionPropertyChanged;
        }

        private void OnSectionPropertyChanged(object sender, PropertyChangedEventArgs e) => RedrawSections();

        private void OnSectionsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (SectionViewModel s in e.OldItems)
                    s.PropertyChanged -= OnSectionPropertyChanged;
            if (e.NewItems != null)
                foreach (SectionViewModel s in e.NewItems)
                    s.PropertyChanged += OnSectionPropertyChanged;
            RedrawSections();
        }
        private void OnTimelinePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TimelineViewModel.ZoomLevel) ||
                e.PropertyName == nameof(TimelineViewModel.ScrollOffsetSamples))
            {
                RedrawSections();
            }
            else if (e.PropertyName == nameof(TimelineViewModel.PlayheadSample))
            {
                UpdatePlayheadHover();
            }
        }

        private void UpdatePlayheadHover()
        {
            if (VM == null) return;
            int ph = VM.Timeline.PlayheadSample;
            SectionViewModel hit = VM.Sections.FirstOrDefault(s => ph >= s.StartSample && ph <= s.EndSample);
            if (hit != _playheadHoveredSection)
            {
                _playheadHoveredSection = hit;
                RedrawSections();
            }
        }

        private void RedrawSections()
        {
            SectionCanvas.Children.Clear();
            if (VM == null) return;

            double viewportWidth = ActualWidth > 0 ? ActualWidth : 1400;

            foreach (var section in VM.Sections)
            {
                double x = VM.Timeline.SampleToPixelX(section.StartSample);
                double w = Math.Max(6, section.Length * VM.Timeline.ZoomLevel);
                double endX = x + w;

                if (endX < 0 || x > viewportWidth) continue;

                bool isHighlighted = section == _hoveredSection || section == _playheadHoveredSection;

                var border = new Border
                {
                    Width = w,
                    Height = 36,
                    Background = GetIntensityBrush(section.Intensity, isHighlighted),
                    BorderBrush = section.IsSelected ? Brushes.Cyan : Brushes.Transparent,
                    BorderThickness = new Thickness(section.IsSelected ? 2 : 0),
                    Tag = section,
                    ToolTip = $"{GetIntensityLabel(section.Intensity)} | Start: {section.StartSample} | Length: {section.Length}{(section.IsAngelJump ? " | Angel Jump" : "")}"
                };

                var label = new TextBlock
                {
                    Text = GetIntensityLabel(section.Intensity) + (section.IsAngelJump ? " \u2726" : ""),
                    Foreground = Brushes.White,
                    FontSize = 11,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                border.Child = label;

                Canvas.SetLeft(border, x);
                Canvas.SetTop(border, 2);
                SectionCanvas.Children.Add(border);
            }
        }

        private static SolidColorBrush GetIntensityBrush(IntensityLevel level, bool highlighted = false)
        {
            byte lift = highlighted ? (byte)50 : (byte)0;
            byte alpha = highlighted ? (byte)230 : (byte)180;
            switch (level)
            {
                case IntensityLevel.Low: return new SolidColorBrush(Color.FromArgb(alpha,
                    (byte)Math.Min(255, 0x2E + lift), (byte)Math.Min(255, 0xCC + lift), (byte)Math.Min(255, 0x71 + lift)));
                case IntensityLevel.Normal: return new SolidColorBrush(Color.FromArgb(alpha,
                    (byte)Math.Min(255, 0x4A + lift), (byte)Math.Min(255, 0x90 + lift), (byte)Math.Min(255, 0xD9 + lift)));
                case IntensityLevel.High: return new SolidColorBrush(Color.FromArgb(alpha,
                    (byte)Math.Min(255, 0xE9 + lift), (byte)Math.Min(255, 0x45 + lift), (byte)Math.Min(255, 0x60 + lift)));
                case IntensityLevel.Extreme: return new SolidColorBrush(Color.FromArgb(alpha,
                    (byte)Math.Min(255, 0x9B + lift), (byte)Math.Min(255, 0x59 + lift), (byte)Math.Min(255, 0xB6 + lift)));
                default: return new SolidColorBrush(Color.FromArgb(alpha,
                    (byte)Math.Min(255, 0x4A + lift), (byte)Math.Min(255, 0x90 + lift), (byte)Math.Min(255, 0xD9 + lift)));
            }
        }

        private static string GetIntensityLabel(IntensityLevel level)
        {
            switch (level)
            {
                case IntensityLevel.Low: return "L";
                case IntensityLevel.Normal: return "N";
                case IntensityLevel.High: return "H";
                case IntensityLevel.Extreme: return "E";
                default: return "N";
            }
        }

        private static IntensityLevel ToolToIntensity(ActiveTool tool)
        {
            switch (tool)
            {
                case ActiveTool.PlaceSectionLow: return IntensityLevel.Low;
                case ActiveTool.PlaceSectionNormal: return IntensityLevel.Normal;
                case ActiveTool.PlaceSectionHigh: return IntensityLevel.High;
                case ActiveTool.PlaceSectionExtreme: return IntensityLevel.Extreme;
                default: return IntensityLevel.Normal;
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (VM == null) return;
            var pos = e.GetPosition(SectionCanvas);
            _dragStart = pos;

            var tool = VM.Timeline.ActiveTool;
            if (tool == ActiveTool.PlaceSectionLow || tool == ActiveTool.PlaceSectionNormal ||
                tool == ActiveTool.PlaceSectionHigh || tool == ActiveTool.PlaceSectionExtreme)
            {
                _isCreating = true;
                _createStartSample = VM.Timeline.PixelXToSample(pos.X);
                CaptureMouse();
                e.Handled = true;
                return;
            }

            var hit = HitTestSection(pos);
            if (hit != null)
            {
                VM.Selection.Select(hit);
                _dragSection = hit;
                _dragStartSample = VM.Timeline.PixelXToSample(pos.X);
                _originalStartSample = hit.StartSample;
                _originalLength = hit.Length;

                double rightEdgeX = VM.Timeline.SampleToPixelX(hit.StartSample + hit.Length);
                _isResizing = Math.Abs(pos.X - rightEdgeX) < 6;
                _isDragging = !_isResizing;

                CaptureMouse();
            }
            else
            {
                VM.Selection.Clear();
            }

            RedrawSections();
            e.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (VM == null) return;

            var pos = e.GetPosition(SectionCanvas);

            if (_isDragging && _dragSection != null)
            {
                int currentSample = VM.Timeline.PixelXToSample(pos.X);
                int delta = currentSample - _dragStartSample;
                _dragSection.StartSample = Math.Max(0, _originalStartSample + delta);
                RedrawSections();
                e.Handled = true;
            }
            else if (_isResizing && _dragSection != null)
            {
                int currentSample = VM.Timeline.PixelXToSample(pos.X);
                int newLength = currentSample - _dragSection.StartSample;
                _dragSection.Length = Math.Max(1, newLength);
                RedrawSections();
                e.Handled = true;
            }
            else if (!_isCreating)
            {
                var hit = HitTestSection(pos);
                if (_hoveredSection != hit)
                {
                    _hoveredSection = hit;
                    RedrawSections();
                }
                if (hit != null)
                {
                    double rightEdgeX = VM.Timeline.SampleToPixelX(hit.StartSample + hit.Length);
                    double leftEdgeX = VM.Timeline.SampleToPixelX(hit.StartSample);
                    if (Math.Abs(pos.X - rightEdgeX) < 6 || Math.Abs(pos.X - leftEdgeX) < 6)
                        Cursor = Cursors.SizeWE;
                    else
                        Cursor = Cursors.Arrow;
                }
                else
                {
                    Cursor = Cursors.Arrow;
                }
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (VM == null)
            {
                if (_isDragging || _isResizing || _isCreating)
                    ReleaseMouseCapture();
                _isDragging = false;
                _isResizing = false;
                _isCreating = false;
                _dragSection = null;
                return;
            }

            if (_isCreating)
            {
                var pos = e.GetPosition(SectionCanvas);
                int endSample = VM.Timeline.PixelXToSample(pos.X);
                int start = Math.Min(_createStartSample, endSample);
                int length = Math.Max(1, Math.Abs(endSample - _createStartSample));

                var model = new IntensitySection
                {
                    Intensity = ToolToIntensity(VM.Timeline.ActiveTool),
                    StartSample = start,
                    Length = length,
                    IsAngelJump = false
                };

                var sectionVm = new SectionViewModel(model);
                VM.ExecuteCommand(new AddSectionCommand(sectionVm, VM.Sections));
                VM.Selection.Select(sectionVm);
                VM.Timeline.RevertToolIfNeeded();
                _isCreating = false;
                ReleaseMouseCapture();
                RedrawSections();
                e.Handled = true;
                return;
            }

            if (_isDragging && _dragSection != null && _dragSection.StartSample != _originalStartSample)
            {
                var cmd = new MoveSectionCommand(_dragSection, _originalStartSample, _dragSection.StartSample);
                VM.ExecuteCommand(cmd);
            }
            else if (_isResizing && _dragSection != null && _dragSection.Length != _originalLength)
            {
                var cmd = new ResizeSectionCommand(_dragSection, _originalStartSample, _originalLength, _dragSection.StartSample, _dragSection.Length);
                VM.ExecuteCommand(cmd);
            }

            _isDragging = false;
            _isResizing = false;
            _dragSection = null;
            ReleaseMouseCapture();
            e.Handled = true;
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            if (VM == null) return;
            var pos = e.GetPosition(SectionCanvas);
            var hit = HitTestSection(pos);
            if (hit == null) return;

            VM.Selection.Select(hit);
            RedrawSections();

            var menu = new ContextMenu();
            var deleteItem = new MenuItem { Header = L.T("Ctx_Delete") };
            deleteItem.Click += (s, ev) =>
            {
                VM.Selection.Clear();
                var cmd = new DeleteSectionCommand(hit, VM.Sections);
                VM.ExecuteCommand(cmd);
            };
            menu.Items.Add(deleteItem);

            var typeMenu = new MenuItem { Header = L.T("Ctx_ChangeType") };
            foreach (IntensityLevel level in Enum.GetValues(typeof(IntensityLevel)))
            {
                var item = new MenuItem { Header = level.ToString(), IsChecked = hit.Intensity == level };
                var capturedLevel = level;
                item.Click += (s, ev) =>
                {
                    var cmd = new ModifySectionCommand(hit, capturedLevel, hit.IsAngelJump);
                    VM.ExecuteCommand(cmd);
                };
                typeMenu.Items.Add(item);
            }
            menu.Items.Add(typeMenu);

            var angelItem = new MenuItem { Header = L.T("Ctx_AngelJump"), IsCheckable = true, IsChecked = hit.IsAngelJump };
            angelItem.Click += (s, ev) =>
            {
                var cmd = new ModifySectionCommand(hit, hit.Intensity, !hit.IsAngelJump);
                VM.ExecuteCommand(cmd);
            };
            menu.Items.Add(angelItem);

            menu.IsOpen = true;
            e.Handled = true;
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            if (_hoveredSection != null)
            {
                _hoveredSection = null;
                RedrawSections();
            }
            base.OnMouseLeave(e);
        }

        private SectionViewModel HitTestSection(Point pos)
        {
            if (VM == null) return null;
            int sampleAtPoint = VM.Timeline.PixelXToSample(pos.X);
            return VM.Sections.FirstOrDefault(s => sampleAtPoint >= s.StartSample && sampleAtPoint <= s.EndSample);
        }
    }
}
