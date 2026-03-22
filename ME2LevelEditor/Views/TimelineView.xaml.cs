using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ME2LevelEditor.Controls;
using ME2LevelEditor.Models;
using ME2LevelEditor.Services;
using ME2LevelEditor.ViewModels;
using L = ME2LevelEditor.Services.LocalizationService;

namespace ME2LevelEditor.Views
{
    public partial class TimelineView : UserControl
    {
        private MainViewModel VM => DataContext as MainViewModel;
        private bool _isDragging;
        private bool _isMultiDragging;
        private bool _isResizingHeld;
        private bool _isRectSelecting;
        private Point _dragStartPoint;
        private int _dragStartSampleId;
        private int _originalSampleId;
        private int? _originalHeldDuration;
        private ObstacleViewModel _dragObstacle;
        private int _rectSelectStartSample;
        private double _rectSelectStartY;
        private List<KeyValuePair<ObstacleViewModel, int>> _multiDragOriginals;

        public TimelineView()
        {
            InitializeComponent();
            SizeChanged += OnSizeChanged;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (VM != null)
                VM.Timeline.ViewportWidthPixels = ActualWidth;
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (VM == null) return;

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                VM.Timeline.ScrollBy(-e.Delta);
            }
            else
            {
                var pos = e.GetPosition(this);
                VM.Timeline.ZoomAtPoint(pos.X, e.Delta);
            }

            e.Handled = true;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (VM == null) return;
            var pos = e.GetPosition(Timeline);
            _dragStartPoint = pos;

            var tool = VM.Timeline.ActiveTool;

            if (tool == ActiveTool.PlaceZone || tool == ActiveTool.PlaceSolid || tool == ActiveTool.PlaceSolidHeld)
            {
                int sampleId = VM.Timeline.PixelXToSample(pos.X);
                var obstacleType = tool == ActiveTool.PlaceZone ? ObstacleType.Zone : ObstacleType.Solid;
                int? heldDuration = tool == ActiveTool.PlaceSolidHeld ? (int?)20 : null;

                var model = new ObstacleDefinition
                {
                    SampleId = sampleId,
                    Type = obstacleType,
                    HeldDuration = heldDuration
                };

                var obstacleVm = new ObstacleViewModel(model);
                VM.ExecuteCommand(new AddObstacleCommand(obstacleVm, VM.Obstacles));
                VM.Selection.Select(obstacleVm);
                VM.Timeline.RevertToolIfNeeded();
                e.Handled = true;
                return;
            }

            if (tool == ActiveTool.PlaceSectionLow || tool == ActiveTool.PlaceSectionNormal ||
                tool == ActiveTool.PlaceSectionHigh || tool == ActiveTool.PlaceSectionExtreme)
            {
                e.Handled = true;
                return;
            }

            var hitObs = Timeline.HitTestObstacle(pos);
            if (hitObs != null)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    VM.Selection.ToggleSelect(hitObs);
                }
                else if (!hitObs.IsSelected)
                {
                    VM.Selection.Select(hitObs);
                }

                var zone = Timeline.HitTestObstacleZone(pos);
                if (zone == HitZone.RightEdge && hitObs.IsHeld)
                {
                    _isResizingHeld = true;
                    _dragObstacle = hitObs;
                    _originalHeldDuration = hitObs.HeldDuration;
                    _dragStartSampleId = VM.Timeline.PixelXToSample(pos.X);
                }
                else if (VM.Selection.SelectedObstacles.Count > 1)
                {
                    _isMultiDragging = true;
                    _dragStartSampleId = VM.Timeline.PixelXToSample(pos.X);
                    _multiDragOriginals = VM.Selection.SelectedObstacles
                        .Select(o => new KeyValuePair<ObstacleViewModel, int>(o, o.SampleId))
                        .ToList();
                }
                else
                {
                    _isDragging = true;
                    _dragObstacle = hitObs;
                    _originalSampleId = hitObs.SampleId;
                    _dragStartSampleId = VM.Timeline.PixelXToSample(pos.X);
                }

                CaptureMouse();
            }
            else
            {
                VM.Selection.Clear();
                int sample = VM.Timeline.PixelXToSample(pos.X);
                VM.Timeline.PlayheadSample = sample;
                if (VM.Audio.IsAudioLoaded)
                {
                    double seconds = VM.Timeline.SampleToSeconds(sample);
                    VM.Audio.SeekTo(seconds);
                }

                _isRectSelecting = true;
                _rectSelectStartSample = sample;
                _rectSelectStartY = pos.Y;
                CaptureMouse();
            }

            e.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (VM == null) return;
            var pos = e.GetPosition(Timeline);

            if (_isDragging && _dragObstacle != null)
            {
                int currentSample = VM.Timeline.PixelXToSample(pos.X);
                int delta = currentSample - _dragStartSampleId;
                _dragObstacle.SampleId = Math.Max(0, _originalSampleId + delta);
                Timeline.Redraw();
                e.Handled = true;
            }
            else if (_isMultiDragging && _multiDragOriginals != null)
            {
                int currentSample = VM.Timeline.PixelXToSample(pos.X);
                int delta = currentSample - _dragStartSampleId;
                foreach (var kv in _multiDragOriginals)
                    kv.Key.SampleId = Math.Max(0, kv.Value + delta);
                Timeline.Redraw();
                e.Handled = true;
            }
            else if (_isResizingHeld && _dragObstacle != null)
            {
                int currentSample = VM.Timeline.PixelXToSample(pos.X);
                int newDuration = Math.Max(1, currentSample - _dragObstacle.SampleId);
                _dragObstacle.HeldDuration = newDuration;
                Timeline.Redraw();
                e.Handled = true;
            }
            else if (_isRectSelecting)
            {
                int currentSample = VM.Timeline.PixelXToSample(pos.X);
                int minSample = Math.Min(_rectSelectStartSample, currentSample);
                int maxSample = Math.Max(_rectSelectStartSample, currentSample);

                double startX = VM.Timeline.SampleToPixelX(minSample);
                double endX = VM.Timeline.SampleToPixelX(maxSample);
                double minY = Math.Min(_rectSelectStartY, pos.Y);
                double maxY = Math.Max(_rectSelectStartY, pos.Y);

                Timeline.SetSelectionRect(new Rect(startX, minY, endX - startX, maxY - minY));

                // Live-highlight obstacles inside the rectangle
                VM.Selection.Clear();
                if (Math.Abs(maxSample - minSample) > 2)
                {
                    var inRange = new List<ObstacleViewModel>();
                    foreach (var obs in VM.Obstacles)
                    {
                        if (obs.SampleId >= minSample && obs.SampleId <= maxSample)
                            inRange.Add(obs);
                    }
                    if (inRange.Count > 0)
                        VM.Selection.SelectRange(inRange);
                }

                e.Handled = true;
            }
            else
            {
                var hitObs = Timeline.HitTestObstacle(pos);
                Timeline.HoveredObstacle = hitObs;

                var zone = Timeline.HitTestObstacleZone(pos);
                if (zone == HitZone.RightEdge)
                    Cursor = Cursors.SizeWE;
                else
                    Cursor = Cursors.Arrow;
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (VM == null)
            {
                if (_isDragging || _isMultiDragging || _isResizingHeld || _isRectSelecting)
                    ReleaseMouseCapture();
                _isDragging = false;
                _isMultiDragging = false;
                _isResizingHeld = false;
                _isRectSelecting = false;
                _dragObstacle = null;
                _multiDragOriginals = null;
                return;
            }

            if (_isDragging && _dragObstacle != null)
            {
                if (_dragObstacle.SampleId != _originalSampleId)
                {
                    var cmd = new MoveObstacleCommand(_dragObstacle, _originalSampleId, _dragObstacle.SampleId);
                    VM.ExecuteCommand(cmd);
                }
                _isDragging = false;
                _dragObstacle = null;
                ReleaseMouseCapture();
            }
            else if (_isMultiDragging && _multiDragOriginals != null)
            {
                bool anyMoved = _multiDragOriginals.Any(kv => kv.Key.SampleId != kv.Value);
                if (anyMoved)
                {
                    var commands = new List<IEditCommand>();
                    foreach (var kv in _multiDragOriginals)
                    {
                        if (kv.Key.SampleId != kv.Value)
                            commands.Add(new MoveObstacleCommand(kv.Key, kv.Value, kv.Key.SampleId));
                    }
                    VM.ExecuteCommand(new CompositeCommand("Move obstacles", commands));
                }
                _isMultiDragging = false;
                _multiDragOriginals = null;
                ReleaseMouseCapture();
            }
            else if (_isResizingHeld && _dragObstacle != null)
            {
                if (_dragObstacle.HeldDuration != _originalHeldDuration)
                {
                    var cmd = new ModifyHeldDurationCommand(_dragObstacle, _originalHeldDuration, _dragObstacle.HeldDuration);
                    VM.ExecuteCommand(cmd);
                }
                _isResizingHeld = false;
                _dragObstacle = null;
                ReleaseMouseCapture();
            }
            else if (_isRectSelecting)
            {
                Timeline.SetSelectionRect(null);
                _isRectSelecting = false;
                ReleaseMouseCapture();
            }

            e.Handled = true;
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            if (VM == null) return;
            var pos = e.GetPosition(Timeline);
            var hit = Timeline.HitTestObstacle(pos);
            if (hit == null) return;

            if (!hit.IsSelected)
                VM.Selection.Select(hit);

            var menu = new ContextMenu();

            var deleteItem = new MenuItem { Header = L.T("Ctx_Delete") };
            deleteItem.Click += (s, ev) =>
            {
                var selected = VM.Selection.SelectedObstacles.ToList();
                VM.Selection.Clear();
                if (selected.Count > 1)
                {
                    var commands = new List<IEditCommand>();
                    foreach (var obs in selected)
                        commands.Add(new DeleteObstacleCommand(obs, VM.Obstacles));
                    VM.ExecuteCommand(new CompositeCommand("Delete selection", commands));
                }
                else
                {
                    var cmd = new DeleteObstacleCommand(hit, VM.Obstacles);
                    VM.ExecuteCommand(cmd);
                }
            };
            menu.Items.Add(deleteItem);
            menu.Items.Add(new Separator());

            var zoneItem = new MenuItem { Header = L.T("Ctx_ChangeToZone"), IsChecked = hit.Type == ObstacleType.Zone };
            zoneItem.Click += (s, ev) =>
            {
                var cmd = new ModifyObstacleTypeCommand(hit, ObstacleType.Zone, null);
                VM.ExecuteCommand(cmd);
            };
            menu.Items.Add(zoneItem);

            var solidItem = new MenuItem { Header = L.T("Ctx_ChangeToSolid"), IsChecked = hit.Type == ObstacleType.Solid && !hit.IsHeld };
            solidItem.Click += (s, ev) =>
            {
                var cmd = new ModifyObstacleTypeCommand(hit, ObstacleType.Solid, null);
                VM.ExecuteCommand(cmd);
            };
            menu.Items.Add(solidItem);

            var heldItem = new MenuItem { Header = L.T("Ctx_ChangeToHeld"), IsChecked = hit.IsHeld };
            heldItem.Click += (s, ev) =>
            {
                var cmd = new ModifyObstacleTypeCommand(hit, ObstacleType.Solid, hit.HeldDuration ?? 20);
                VM.ExecuteCommand(cmd);
            };
            menu.Items.Add(heldItem);

            menu.IsOpen = true;
            e.Handled = true;
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            Timeline.HoveredObstacle = null;
            base.OnMouseLeave(e);
        }
    }
}
