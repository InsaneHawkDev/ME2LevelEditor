using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ME2LevelEditor.Models;
using ME2LevelEditor.Services;

namespace ME2LevelEditor.Views
{
    public partial class KeyMappingDialog : Window
    {
        private readonly KeyMappingService _service;
        private List<KeyMappingEntry> _entries;
        private readonly Dictionary<PlacementAction, Key> _snapshot;

        public KeyMappingDialog(KeyMappingService service)
        {
            InitializeComponent();
            _service = service;
            _snapshot = new Dictionary<PlacementAction, Key>();
            foreach (var entry in _service.GetAllMappings())
                _snapshot[entry.Action] = (Key)System.Enum.Parse(typeof(Key), entry.KeyName);
            RefreshList();
        }

        private void RefreshList()
        {
            _entries = _service.GetAllMappings();
            MappingsList.ItemsSource = null;
            MappingsList.ItemsSource = _entries;
        }

        private void OnKeyBindingKeyDown(object sender, KeyEventArgs e)
        {
            var textBox = sender as TextBox;
            var entry = textBox?.Tag as KeyMappingEntry;
            if (entry == null) return;

            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key == Key.Escape || key == Key.Tab) return;

            _service.SetMapping(entry.Action, key);
            RefreshList();
            e.Handled = true;
        }

        private void OnKeyBindingGotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
                textBox.BorderBrush = (Brush)FindResource("AccentCyan");
        }

        private void OnKeyBindingLostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
                textBox.BorderBrush = (Brush)FindResource("BorderMain");
        }

        private void OnResetDefaults(object sender, RoutedEventArgs e)
        {
            _service.SetMapping(PlacementAction.PlaceZone, Key.A);
            _service.SetMapping(PlacementAction.PlaceSolid, Key.S);
            _service.SetMapping(PlacementAction.PlaceSectionLow, Key.F1);
            _service.SetMapping(PlacementAction.PlaceSectionNormal, Key.F2);
            _service.SetMapping(PlacementAction.PlaceSectionHigh, Key.F3);
            _service.SetMapping(PlacementAction.PlaceSectionExtreme, Key.F4);
            RefreshList();
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            _service.Save();
            DialogResult = true;
            Close();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            foreach (var kv in _snapshot)
                _service.SetMapping(kv.Key, kv.Value);
            DialogResult = false;
            Close();
        }
    }
}
