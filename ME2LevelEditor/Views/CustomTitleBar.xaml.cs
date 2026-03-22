using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ME2LevelEditor.Views
{
    public partial class CustomTitleBar : UserControl
    {
        public static readonly DependencyProperty ShowMaximizeProperty =
            DependencyProperty.Register(nameof(ShowMaximize), typeof(bool), typeof(CustomTitleBar),
                new PropertyMetadata(true, OnShowButtonsChanged));

        public static readonly DependencyProperty ShowMinimizeProperty =
            DependencyProperty.Register(nameof(ShowMinimize), typeof(bool), typeof(CustomTitleBar),
                new PropertyMetadata(true, OnShowButtonsChanged));

        public bool ShowMaximize
        {
            get => (bool)GetValue(ShowMaximizeProperty);
            set => SetValue(ShowMaximizeProperty, value);
        }

        public bool ShowMinimize
        {
            get => (bool)GetValue(ShowMinimizeProperty);
            set => SetValue(ShowMinimizeProperty, value);
        }

        private Window _subscribedWindow;

        public CustomTitleBar()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _subscribedWindow = Window.GetWindow(this);
            if (_subscribedWindow != null)
            {
                TitleText.SetBinding(TextBlock.TextProperty,
                    new System.Windows.Data.Binding("Title") { Source = _subscribedWindow });
                _subscribedWindow.StateChanged += OnWindowStateChanged;
                UpdateMaximizeIcon(_subscribedWindow);
            }
            UpdateButtonVisibility();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_subscribedWindow != null)
            {
                _subscribedWindow.StateChanged -= OnWindowStateChanged;
                _subscribedWindow = null;
            }
        }

        private static void OnShowButtonsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((CustomTitleBar)d).UpdateButtonVisibility();
        }

        private void UpdateButtonVisibility()
        {
            if (MinimizeButton != null)
                MinimizeButton.Visibility = ShowMinimize ? Visibility.Visible : Visibility.Collapsed;
            if (MaximizeButton != null)
                MaximizeButton.Visibility = ShowMaximize ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnWindowStateChanged(object sender, EventArgs e)
        {
            UpdateMaximizeIcon((Window)sender);
        }

        private void UpdateMaximizeIcon(Window window)
        {
            MaximizeButton.Content = window.WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
        }

        private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window == null) return;

            if (e.ClickCount == 2 && ShowMaximize)
            {
                window.WindowState = window.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (window.WindowState == WindowState.Maximized)
                {
                    var point = e.GetPosition(window);
                    var screenPoint = window.PointToScreen(point);
                    double ratioX = point.X / window.ActualWidth;

                    window.WindowState = WindowState.Normal;

                    window.Left = screenPoint.X - window.Width * ratioX;
                    window.Top = screenPoint.Y - e.GetPosition(this).Y;
                }
                window.DragMove();
            }
        }

        private void OnMinimize(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
                window.WindowState = WindowState.Minimized;
        }

        private void OnMaximize(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
                window.WindowState = window.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
        }

        private void OnClose(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }
    }
}
