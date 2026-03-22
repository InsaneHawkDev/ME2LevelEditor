using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using ME2LevelEditor.Models;
using ME2LevelEditor.ViewModels;

namespace ME2LevelEditor.Controls
{
    public enum HitZone
    {
        None,
        Body,
        RightEdge
    }

    public class TimelineCanvas : FrameworkElement
    {
        private static readonly SolidColorBrush s_selBrush;
        private static readonly Pen s_selPen;
        private static readonly SolidColorBrush s_whiteBrush;
        private static readonly Pen s_stripePen;
        private static readonly SolidColorBrush s_ignoredLabelBrush;
        private static readonly SolidColorBrush s_playheadBrush;
        private static readonly Pen s_playheadPen;
        private static readonly SolidColorBrush s_dimLabelBrush;
        private static readonly SolidColorBrush s_selRectFill;
        private static readonly Pen s_selRectBorderPen;
        private static readonly Pen s_arrowPen;

        static TimelineCanvas()
        {
            s_selBrush = new SolidColorBrush(Color.FromRgb(0x00, 0xC8, 0xFF)); s_selBrush.Freeze();
            s_selPen = new Pen(s_selBrush, 2); s_selPen.Freeze();
            s_whiteBrush = new SolidColorBrush(Colors.White); s_whiteBrush.Freeze();
            s_stripePen = new Pen(new SolidColorBrush(Color.FromArgb(80, 255, 160, 0)), 1); s_stripePen.Freeze();
            s_ignoredLabelBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xA0, 0x00)); s_ignoredLabelBrush.Freeze();
            s_playheadBrush = new SolidColorBrush(Color.FromRgb(0x00, 0xC8, 0xFF)); s_playheadBrush.Freeze();
            s_playheadPen = new Pen(s_playheadBrush, 2); s_playheadPen.Freeze();
            s_dimLabelBrush = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)); s_dimLabelBrush.Freeze();
            s_selRectFill = new SolidColorBrush(Color.FromArgb(30, 0x00, 0xC8, 0xFF)); s_selRectFill.Freeze();
            var selRectBorderBrush = new SolidColorBrush(Color.FromArgb(140, 0x00, 0xC8, 0xFF)); selRectBorderBrush.Freeze();
            s_selRectBorderPen = new Pen(selRectBorderBrush, 1); s_selRectBorderPen.DashStyle = DashStyles.Dash; s_selRectBorderPen.Freeze();
            s_arrowPen = new Pen(s_dimLabelBrush, 1.5); s_arrowPen.Freeze();
        }

        private readonly List<Visual> _visuals = new List<Visual>();
        private DrawingVisual _contentVisual;
        private DrawingVisual _playheadVisual;
        private DrawingVisual _selectionRectVisual;
        private ObstacleViewModel _playheadHoveredObstacle;

        public static readonly DependencyProperty ObstaclesProperty =
            DependencyProperty.Register(nameof(Obstacles), typeof(IEnumerable<ObstacleViewModel>), typeof(TimelineCanvas),
                new PropertyMetadata(null, OnObstaclesChanged));

        public static readonly DependencyProperty SectionsProperty =
            DependencyProperty.Register(nameof(Sections), typeof(IEnumerable<SectionViewModel>), typeof(TimelineCanvas),
                new PropertyMetadata(null, OnSectionsChanged));

        public static readonly DependencyProperty ZoomLevelProperty =
            DependencyProperty.Register(nameof(ZoomLevel), typeof(double), typeof(TimelineCanvas),
                new PropertyMetadata(1.0, OnRenderPropertyChanged));

        public static readonly DependencyProperty ScrollOffsetSamplesProperty =
            DependencyProperty.Register(nameof(ScrollOffsetSamples), typeof(int), typeof(TimelineCanvas),
                new PropertyMetadata(0, OnRenderPropertyChanged));

        public static readonly DependencyProperty TotalSamplesProperty =
            DependencyProperty.Register(nameof(TotalSamples), typeof(int), typeof(TimelineCanvas),
                new PropertyMetadata(0, OnRenderPropertyChanged));

        public static readonly DependencyProperty PlayheadSampleProperty =
            DependencyProperty.Register(nameof(PlayheadSample), typeof(int), typeof(TimelineCanvas),
                new PropertyMetadata(0, OnPlayheadChanged));

        public static readonly DependencyProperty HoveredObstacleProperty =
            DependencyProperty.Register(nameof(HoveredObstacle), typeof(ObstacleViewModel), typeof(TimelineCanvas),
                new PropertyMetadata(null, OnRenderPropertyChanged));

        public static readonly DependencyProperty ViewportWidthProperty =
            DependencyProperty.Register(nameof(ViewportWidth), typeof(double), typeof(TimelineCanvas),
                new PropertyMetadata(0.0, OnRenderPropertyChanged));

        public static readonly DependencyProperty DurationSecondsProperty =
            DependencyProperty.Register(nameof(DurationSeconds), typeof(double), typeof(TimelineCanvas),
                new PropertyMetadata(0.0, OnRenderPropertyChanged));

        public IEnumerable<ObstacleViewModel> Obstacles
        {
            get => (IEnumerable<ObstacleViewModel>)GetValue(ObstaclesProperty);
            set => SetValue(ObstaclesProperty, value);
        }

        public IEnumerable<SectionViewModel> Sections
        {
            get => (IEnumerable<SectionViewModel>)GetValue(SectionsProperty);
            set => SetValue(SectionsProperty, value);
        }

        public double ZoomLevel
        {
            get => (double)GetValue(ZoomLevelProperty);
            set => SetValue(ZoomLevelProperty, value);
        }

        public int ScrollOffsetSamples
        {
            get => (int)GetValue(ScrollOffsetSamplesProperty);
            set => SetValue(ScrollOffsetSamplesProperty, value);
        }

        public int TotalSamples
        {
            get => (int)GetValue(TotalSamplesProperty);
            set => SetValue(TotalSamplesProperty, value);
        }

        public int PlayheadSample
        {
            get => (int)GetValue(PlayheadSampleProperty);
            set => SetValue(PlayheadSampleProperty, value);
        }

        public ObstacleViewModel HoveredObstacle
        {
            get => (ObstacleViewModel)GetValue(HoveredObstacleProperty);
            set => SetValue(HoveredObstacleProperty, value);
        }

        public double ViewportWidth
        {
            get => (double)GetValue(ViewportWidthProperty);
            set => SetValue(ViewportWidthProperty, value);
        }

        public double DurationSeconds
        {
            get => (double)GetValue(DurationSecondsProperty);
            set => SetValue(DurationSecondsProperty, value);
        }

        private Rect? _selectionRect;

        public void SetSelectionRect(Rect? rect)
        {
            _selectionRect = rect;
            RedrawSelectionRect();
        }

        private void RedrawSelectionRect()
        {
            if (_selectionRectVisual != null)
            {
                RemoveVisualChild(_selectionRectVisual);
                _visuals.Remove(_selectionRectVisual);
                _selectionRectVisual = null;
            }

            if (_selectionRect.HasValue)
            {
                _selectionRectVisual = new DrawingVisual();
                using (var dc = _selectionRectVisual.RenderOpen())
                {
                    dc.DrawRectangle(s_selRectFill, s_selRectBorderPen, _selectionRect.Value);
                }
                _visuals.Add(_selectionRectVisual);
                AddVisualChild(_selectionRectVisual);
            }
        }

        public TimelineCanvas()
        {
            SizeChanged += (s, e) => ViewportWidth = ActualWidth;
        }

        protected override int VisualChildrenCount => _visuals.Count;

        protected override Visual GetVisualChild(int index) => _visuals[index];

        private static void OnRenderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TimelineCanvas)d).Redraw();
        }

        private static void OnPlayheadChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var canvas = (TimelineCanvas)d;
            canvas.RedrawPlayhead();
            canvas.UpdatePlayheadHover();
        }

        private void UpdatePlayheadHover()
        {
            var obstacles = Obstacles;
            ObstacleViewModel hit = null;
            if (obstacles != null)
            {
                int ph = PlayheadSample;
                foreach (var obs in obstacles)
                {
                    int obsEnd = obs.IsHeld ? obs.SampleId + (obs.HeldDuration ?? 0) : obs.SampleId;
                    if (ph >= obs.SampleId && ph <= obsEnd)
                    {
                        hit = obs;
                        break;
                    }
                }
            }

            if (hit != _playheadHoveredObstacle)
            {
                _playheadHoveredObstacle = hit;
                Redraw();
            }
        }

        private static void OnObstaclesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var canvas = (TimelineCanvas)d;
            if (e.OldValue is INotifyCollectionChanged oldCollection)
            {
                oldCollection.CollectionChanged -= canvas.OnObstacleCollectionChanged;
                canvas.UnsubscribeObstacles(e.OldValue as IEnumerable<ObstacleViewModel>);
            }
            if (e.NewValue is INotifyCollectionChanged newCollection)
            {
                newCollection.CollectionChanged += canvas.OnObstacleCollectionChanged;
                canvas.SubscribeObstacles(e.NewValue as IEnumerable<ObstacleViewModel>);
            }
            canvas.Redraw();
        }

        private static void OnSectionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var canvas = (TimelineCanvas)d;
            if (e.OldValue is INotifyCollectionChanged oldCollection)
            {
                oldCollection.CollectionChanged -= canvas.OnSectionCollectionChanged;
                canvas.UnsubscribeSections(e.OldValue as IEnumerable<SectionViewModel>);
            }
            if (e.NewValue is INotifyCollectionChanged newCollection)
            {
                newCollection.CollectionChanged += canvas.OnSectionCollectionChanged;
                canvas.SubscribeSections(e.NewValue as IEnumerable<SectionViewModel>);
            }
            canvas.Redraw();
        }

        private void SubscribeObstacles(IEnumerable<ObstacleViewModel> obstacles)
        {
            if (obstacles == null) return;
            foreach (var obs in obstacles)
                obs.PropertyChanged += OnItemPropertyChanged;
        }

        private void UnsubscribeObstacles(IEnumerable<ObstacleViewModel> obstacles)
        {
            if (obstacles == null) return;
            foreach (var obs in obstacles)
                obs.PropertyChanged -= OnItemPropertyChanged;
        }

        private void SubscribeSections(IEnumerable<SectionViewModel> sections)
        {
            if (sections == null) return;
            foreach (var sec in sections)
                sec.PropertyChanged += OnItemPropertyChanged;
        }

        private void UnsubscribeSections(IEnumerable<SectionViewModel> sections)
        {
            if (sections == null) return;
            foreach (var sec in sections)
                sec.PropertyChanged -= OnItemPropertyChanged;
        }

        private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Redraw();
        }

        private void OnObstacleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (ObstacleViewModel obs in e.OldItems)
                    obs.PropertyChanged -= OnItemPropertyChanged;
            if (e.NewItems != null)
                foreach (ObstacleViewModel obs in e.NewItems)
                    obs.PropertyChanged += OnItemPropertyChanged;
            Redraw();
        }

        private void OnSectionCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (SectionViewModel sec in e.OldItems)
                    sec.PropertyChanged -= OnItemPropertyChanged;
            if (e.NewItems != null)
                foreach (SectionViewModel sec in e.NewItems)
                    sec.PropertyChanged += OnItemPropertyChanged;
            Redraw();
        }

        public void Redraw()
        {
            foreach (var visual in _visuals)
                RemoveVisualChild(visual);
            _visuals.Clear();
            _contentVisual = null;
            _playheadVisual = null;
            _selectionRectVisual = null;

            if (ViewportWidth <= 0 || ZoomLevel <= 0)
                return;

            _contentVisual = new DrawingVisual();
            using (var dc = _contentVisual.RenderOpen())
            {
                DrawIntensityBands(dc);
                DrawTimeRuler(dc);
                DrawObstacles(dc);
            }
            _visuals.Add(_contentVisual);
            AddVisualChild(_contentVisual);

            _playheadVisual = new DrawingVisual();
            _visuals.Add(_playheadVisual);
            AddVisualChild(_playheadVisual);
            RedrawPlayhead();
            RedrawSelectionRect();
        }

        private void RedrawPlayhead()
        {
            if (_playheadVisual == null) return;
            using (var dc = _playheadVisual.RenderOpen())
            {
                DrawPlayhead(dc);
            }
        }

        private int VisibleStartSample => ScrollOffsetSamples;
        private int VisibleEndSample => ScrollOffsetSamples + (int)(ViewportWidth / ZoomLevel);

        private void DrawIntensityBands(DrawingContext dc)
        {
            var sections = Sections;
            if (sections == null) return;

            var sectionList = sections.ToList();
            var visibleSections = sectionList.Where(s =>
                s.EndSample > VisibleStartSample && s.StartSample < VisibleEndSample).ToList();

            foreach (var section in visibleSections)
            {
                double x = (section.StartSample - ScrollOffsetSamples) * ZoomLevel;
                double w = section.Length * ZoomLevel;
                Color bandColor = GetIntensityColor(section.Intensity);
                var brush = new SolidColorBrush(bandColor);
                brush.Freeze();
                dc.DrawRectangle(brush, null, new Rect(x, 0, w, ActualHeight));

                bool overlaps = sectionList.Any(other =>
                    other != section &&
                    other.StartSample < section.EndSample &&
                    other.EndSample > section.StartSample);

                if (overlaps)
                {
                    var orangePen = new Pen(new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00)), 1);
                    orangePen.Freeze();
                    dc.DrawRectangle(null, orangePen, new Rect(x, 0, w, ActualHeight));
                }
            }
        }

        private Color GetIntensityColor(IntensityLevel intensity)
        {
            switch (intensity)
            {
                case IntensityLevel.Low: return Color.FromArgb(38, 0x2E, 0xCC, 0x71);
                case IntensityLevel.Normal: return Color.FromArgb(38, 0x4A, 0x90, 0xD9);
                case IntensityLevel.High: return Color.FromArgb(38, 0xE9, 0x45, 0x60);
                case IntensityLevel.Extreme: return Color.FromArgb(38, 0x9B, 0x59, 0xB6);
                default: return Color.FromArgb(38, 0x4A, 0x90, 0xD9);
            }
        }

        private void DrawTimeRuler(DrawingContext dc)
        {
            var rulerBrush = new SolidColorBrush(Color.FromRgb(0x6a, 0x7a, 0x9a));
            rulerBrush.Freeze();
            var rulerPen = new Pen(rulerBrush, 1);
            rulerPen.Freeze();

            double pixelsPerSecond = TotalSamples > 0 && DurationSeconds > 0
                ? ZoomLevel * TotalSamples / DurationSeconds
                : ZoomLevel;

            double minTickPixels = 80;
            double[] intervals = { 0.1, 0.25, 0.5, 1, 2, 5, 10, 15, 30, 60, 120, 300 };
            double tickIntervalSeconds = intervals[intervals.Length - 1];
            foreach (double interval in intervals)
            {
                if (interval * pixelsPerSecond >= minTickPixels)
                {
                    tickIntervalSeconds = interval;
                    break;
                }
            }

            double totalDuration = DurationSeconds > 0 ? DurationSeconds : 1;
            double startSeconds = TotalSamples > 0
                ? (double)VisibleStartSample / TotalSamples * totalDuration
                : 0;
            double endSeconds = TotalSamples > 0
                ? (double)VisibleEndSample / TotalSamples * totalDuration
                : 0;

            double firstTick = Math.Floor(startSeconds / tickIntervalSeconds) * tickIntervalSeconds;

            var typeface = new Typeface("Segoe UI");

            for (double t = firstTick; t <= endSeconds + tickIntervalSeconds; t += tickIntervalSeconds)
            {
                if (t < 0) continue;
                int sample = TotalSamples > 0 ? (int)(t / totalDuration * TotalSamples) : 0;
                double x = (sample - ScrollOffsetSamples) * ZoomLevel;

                if (x < -50 || x > ViewportWidth + 50) continue;

                dc.DrawLine(rulerPen, new Point(x, 0), new Point(x, 20));

                int minutes = (int)(t / 60);
                double secs = t - minutes * 60;
                string label = $"{minutes:D2}:{secs:00.0}";

                var text = new FormattedText(
                    label,
                    System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    10,
                    rulerBrush,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                dc.DrawText(text, new Point(x + 2, 2));
            }

            dc.DrawLine(rulerPen, new Point(0, 20), new Point(ViewportWidth, 20));
        }

        private static Color Lighten(Color c, byte amount)
        {
            return Color.FromArgb(c.A,
                (byte)Math.Min(255, c.R + amount),
                (byte)Math.Min(255, c.G + amount),
                (byte)Math.Min(255, c.B + amount));
        }

        private void DrawObstacles(DrawingContext dc)
        {
            var obstacles = Obstacles;
            if (obstacles == null) return;

            double rulerHeight = 20;
            double contentHeight = ActualHeight - rulerHeight;
            var typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
            double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            var hovered = HoveredObstacle;
            const byte hoverLift = 50;

            var sections = Sections;
            List<SectionViewModel> sectionList = null;
            if (sections != null)
                sectionList = sections.ToList();

            List<ObstacleZone> obstacleZones = null;
            if (sectionList != null && sectionList.Count > 0 && TotalSamples > 0)
            {
                var sorted = sectionList.OrderBy(s => s.StartSample).ToList();
                obstacleZones = ComputeObstacleZones(sorted, TotalSamples);
            }

            foreach (var obs in obstacles)
            {
                int endSample = obs.IsHeld ? obs.SampleId + (obs.HeldDuration ?? 0) : obs.SampleId;
                if (endSample < VisibleStartSample || obs.SampleId > VisibleEndSample)
                    continue;

                double x = (obs.SampleId - ScrollOffsetSamples) * ZoomLevel;
                bool isHovered = obs == hovered || obs == _playheadHoveredObstacle;
                bool isIgnored = false;
                if (sectionList != null)
                {
                    foreach (var sec in sectionList)
                    {
                        if (obs.SampleId >= sec.StartSample && obs.SampleId < sec.EndSample)
                        {
                            isIgnored = true;
                            break;
                        }
                    }
                }
                if (!isIgnored && obstacleZones != null)
                    isIgnored = IsObstacleWarned(obs, obstacleZones);

                if (obs.Type == ObstacleType.Zone)
                {
                    var c1 = Color.FromRgb(0x9B, 0x5D, 0xE5);
                    var c2 = Color.FromRgb(0x69, 0x30, 0xC3);
                    if (isIgnored) { c1 = Color.FromRgb(0x5a, 0x5a, 0x5a); c2 = Color.FromRgb(0x3a, 0x3a, 0x3a); }
                    else if (isHovered) { c1 = Lighten(c1, hoverLift); c2 = Lighten(c2, hoverLift); }
                    var brush = new LinearGradientBrush(c1, c2, 90); brush.Freeze();
                    var borderPen = new Pen(new SolidColorBrush(isIgnored ? Color.FromArgb(153, 0xFF, 0xA0, 0x00) : Color.FromArgb(153, c1.R, c1.G, c1.B)), 1); borderPen.Freeze();

                    double rectWidth = 14;
                    double rectHeight = 36;
                    double y = rulerHeight + (contentHeight - rectHeight) / 2;
                    dc.DrawRoundedRectangle(brush, borderPen, new Rect(x, y, rectWidth, rectHeight), 3, 3);

                    if (isIgnored)
                        DrawIgnoredOverlay(dc, x, y, rectWidth, rectHeight, s_stripePen, s_ignoredLabelBrush, typeface, dpi);

                    var labelBrush = isIgnored ? s_dimLabelBrush : s_whiteBrush;
                    var label = new FormattedText("Z", System.Globalization.CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight, typeface, 10, labelBrush, dpi);
                    dc.DrawText(label, new Point(x + (rectWidth - label.Width) / 2, y + (rectHeight - label.Height) / 2));

                    if (obs.IsSelected)
                        dc.DrawRoundedRectangle(null, s_selPen, new Rect(x - 1, y - 1, rectWidth + 2, rectHeight + 2), 3, 3);
                }
                else if (obs.IsHeld)
                {
                    var c1 = Color.FromRgb(0xFF, 0x60, 0x80);
                    var c2 = Color.FromRgb(0xD0, 0x40, 0x50);
                    if (isIgnored) { c1 = Color.FromRgb(0x5a, 0x5a, 0x5a); c2 = Color.FromRgb(0x3a, 0x3a, 0x3a); }
                    else if (isHovered) { c1 = Lighten(c1, hoverLift); c2 = Lighten(c2, hoverLift); }
                    var gradBrush = new LinearGradientBrush(c1, c2, 0); gradBrush.Freeze();
                    var borderPen = new Pen(new SolidColorBrush(isIgnored ? Color.FromArgb(153, 0xFF, 0xA0, 0x00) : Color.FromArgb(153, 0xE9, 0x45, 0x60)), 1); borderPen.Freeze();

                    double rectHeight = 50;
                    double y = rulerHeight + (contentHeight - rectHeight) / 2;
                    double w = Math.Max(20, (obs.HeldDuration ?? 0) * ZoomLevel);

                    var rect = new Rect(x, y, w, rectHeight);
                    dc.DrawRoundedRectangle(gradBrush, borderPen, rect, 3, 3);

                    if (isIgnored)
                        DrawIgnoredOverlay(dc, x, y, w, rectHeight, s_stripePen, s_ignoredLabelBrush, typeface, dpi);

                    if (!isIgnored)
                    {
                        double arrowX = x + w - 6;
                        double arrowMidY = y + rectHeight / 2;
                        dc.DrawLine(s_arrowPen, new Point(arrowX, arrowMidY - 6), new Point(arrowX + 4, arrowMidY));
                        dc.DrawLine(s_arrowPen, new Point(arrowX + 4, arrowMidY), new Point(arrowX, arrowMidY + 6));
                    }

                    var labelBrush2 = isIgnored ? s_dimLabelBrush : s_whiteBrush;
                    var label = new FormattedText("S-H", System.Globalization.CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight, typeface, 9, labelBrush2, dpi);
                    dc.DrawText(label, new Point(x + 3, y + (rectHeight - label.Height) / 2));

                    if (obs.IsSelected)
                        dc.DrawRoundedRectangle(null, s_selPen, new Rect(x - 1, y - 1, w + 2, rectHeight + 2), 3, 3);
                }
                else
                {
                    var c1 = Color.FromRgb(0xF0, 0x50, 0x70);
                    var c2 = Color.FromRgb(0xD6, 0x30, 0x50);
                    if (isIgnored) { c1 = Color.FromRgb(0x5a, 0x5a, 0x5a); c2 = Color.FromRgb(0x3a, 0x3a, 0x3a); }
                    else if (isHovered) { c1 = Lighten(c1, hoverLift); c2 = Lighten(c2, hoverLift); }
                    var brush = new LinearGradientBrush(c1, c2, 90); brush.Freeze();
                    var borderPen = new Pen(new SolidColorBrush(isIgnored ? Color.FromArgb(153, 0xFF, 0xA0, 0x00) : Color.FromArgb(153, c1.R, c1.G, c1.B)), 1); borderPen.Freeze();

                    double rectWidth = 14;
                    double rectHeight = 50;
                    double y = rulerHeight + (contentHeight - rectHeight) / 2;
                    dc.DrawRoundedRectangle(brush, borderPen, new Rect(x, y, rectWidth, rectHeight), 3, 3);

                    if (isIgnored)
                        DrawIgnoredOverlay(dc, x, y, rectWidth, rectHeight, s_stripePen, s_ignoredLabelBrush, typeface, dpi);

                    var labelBrush3 = isIgnored ? s_dimLabelBrush : s_whiteBrush;
                    var label = new FormattedText("S", System.Globalization.CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight, typeface, 10, labelBrush3, dpi);
                    dc.DrawText(label, new Point(x + (rectWidth - label.Width) / 2, y + (rectHeight - label.Height) / 2));

                    if (obs.IsSelected)
                        dc.DrawRoundedRectangle(null, s_selPen, new Rect(x - 1, y - 1, rectWidth + 2, rectHeight + 2), 3, 3);
                }
            }
        }

        private struct ObstacleZone
        {
            public int Start;
            public int End;
            public int ObstaclesStart;
            public int ObstaclesEnd;
        }

        private static List<ObstacleZone> ComputeObstacleZones(List<SectionViewModel> sortedSections, int totalSamples)
        {
            const int MinSectionLen = 85;
            const int AjLowRecoveryLen = 46;
            var zones = new List<ObstacleZone>();

            var currentIntensity = IntensityLevel.Low;
            int zoneStart = 0;

            for (int i = 0; i <= sortedSections.Count; i++)
            {
                int zoneEnd;
                bool intoAJ = false;
                bool isLast = i == sortedSections.Count;

                if (i < sortedSections.Count)
                {
                    zoneEnd = sortedSections[i].StartSample - 1;
                    intoAJ = sortedSections[i].IsAngelJump;
                }
                else
                {
                    zoneEnd = totalSamples - 1;
                }

                bool fromAJ = false;
                if (i > 0)
                {
                    var prev = sortedSections[i - 1];
                    if (prev.IsAngelJump)
                    {
                        fromAJ = true;
                        if (prev.Intensity == IntensityLevel.Low)
                            zoneStart += AjLowRecoveryLen;
                    }
                }

                int zoneLength = zoneEnd - zoneStart + 1;
                if (zoneLength >= MinSectionLen)
                {
                    int obsStart = zoneStart;
                    int obsEnd = zoneEnd - 6;

                    if (intoAJ)
                        obsEnd = zoneEnd - 10;
                    if (isLast)
                        obsEnd = zoneEnd - 120;

                    if (fromAJ)
                    {
                        if (currentIntensity == IntensityLevel.Low) obsStart += 80;
                        else if (currentIntensity == IntensityLevel.Extreme) obsStart += 22;
                        else obsStart += 30;
                    }
                    else if (currentIntensity == IntensityLevel.Low && i > 0)
                    {
                        obsStart += 40;
                    }

                    zones.Add(new ObstacleZone
                    {
                        Start = zoneStart,
                        End = zoneEnd,
                        ObstaclesStart = obsStart,
                        ObstaclesEnd = obsEnd
                    });
                }
                else if (zoneLength >= 1)
                {
                    zones.Add(new ObstacleZone
                    {
                        Start = zoneStart,
                        End = zoneEnd,
                        ObstaclesStart = -1,
                        ObstaclesEnd = -1
                    });
                }

                if (i < sortedSections.Count)
                {
                    currentIntensity = sortedSections[i].Intensity;
                    zoneStart = sortedSections[i].EndSample;
                }
            }

            return zones;
        }

        private static bool IsObstacleWarned(ObstacleViewModel obs, List<ObstacleZone> zones)
        {
            int sample = obs.SampleId;
            foreach (var zone in zones)
            {
                if (sample >= zone.Start && sample <= zone.End)
                {
                    if (zone.ObstaclesStart < 0)
                        return true;
                    if (sample < zone.ObstaclesStart || sample > zone.ObstaclesEnd)
                        return true;
                    if (obs.IsHeld && obs.HeldDuration.HasValue)
                    {
                        int endSample = sample + obs.HeldDuration.Value - 1;
                        if (endSample > zone.ObstaclesEnd)
                            return true;
                    }
                    return false;
                }
            }
            return false;
        }

        private void DrawIgnoredOverlay(DrawingContext dc, double x, double y, double w, double h,
            Pen stripePen, Brush labelBrush, Typeface typeface, double dpi)
        {
            var clipGeometry = new RectangleGeometry(new Rect(x, y, w, h), 3, 3);
            dc.PushClip(clipGeometry);
            double spacing = 6;
            for (double offset = -h; offset < w + h; offset += spacing)
            {
                dc.DrawLine(stripePen,
                    new Point(x + offset, y + h),
                    new Point(x + offset + h, y));
            }
            dc.Pop();

            var warnText = new FormattedText("\u26A0", System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, typeface, 9, labelBrush, dpi);
            dc.DrawText(warnText, new Point(x + (w - warnText.Width) / 2, y + h + 1));
        }

        private void DrawPlayhead(DrawingContext dc)
        {
            double x = (PlayheadSample - ScrollOffsetSamples) * ZoomLevel;
            if (x < -2 || x > ViewportWidth + 2) return;

            dc.DrawLine(s_playheadPen, new Point(x, 0), new Point(x, ActualHeight));

            var triangleGeometry = new StreamGeometry();
            using (var ctx = triangleGeometry.Open())
            {
                ctx.BeginFigure(new Point(x - 5, 0), true, true);
                ctx.LineTo(new Point(x + 5, 0), false, false);
                ctx.LineTo(new Point(x, 8), false, false);
            }
            triangleGeometry.Freeze();
            dc.DrawGeometry(s_playheadBrush, null, triangleGeometry);
        }

        public ObstacleViewModel HitTestObstacle(Point point)
        {
            var obstacles = Obstacles;
            if (obstacles == null) return null;

            double rulerHeight = 20;
            double contentHeight = ActualHeight - rulerHeight;

            ObstacleViewModel closest = null;
            double closestDist = double.MaxValue;

            foreach (var obs in obstacles)
            {
                double rectHeight = obs.Type == ObstacleType.Zone ? 36 : 50;
                double obsY = rulerHeight + (contentHeight - rectHeight) / 2;

                if (point.Y < obsY || point.Y > obsY + rectHeight)
                    continue;

                double obsWidthPx = obs.IsHeld
                    ? Math.Max(20, (obs.HeldDuration ?? 0) * ZoomLevel)
                    : 14;
                double obsXStart = (obs.SampleId - ScrollOffsetSamples) * ZoomLevel;
                double obsXEnd = obsXStart + obsWidthPx;

                if (point.X >= obsXStart - 2 && point.X <= obsXEnd + 2)
                {
                    double dist = 0;
                    if (point.X < obsXStart)
                        dist = obsXStart - point.X;
                    else if (point.X > obsXEnd)
                        dist = point.X - obsXEnd;

                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closest = obs;
                    }
                }
            }

            return closest;
        }

        public HitZone HitTestObstacleZone(Point point)
        {
            var obs = HitTestObstacle(point);
            if (obs == null) return HitZone.None;

            if (obs.IsHeld)
            {
                double obsX = (obs.SampleId - ScrollOffsetSamples) * ZoomLevel;
                double w = Math.Max(20, (obs.HeldDuration ?? 0) * ZoomLevel);
                double endX = obsX + w;
                if (Math.Abs(point.X - endX) <= 5)
                    return HitZone.RightEdge;
            }

            return HitZone.Body;
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }
    }
}
