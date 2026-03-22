using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using ME2LevelEditor.Models;

namespace ME2LevelEditor.Services
{
    public class KeyMappingEntry
    {
        public PlacementAction Action { get; set; }
        public string KeyName { get; set; }
        public string DisplayName { get; set; }
    }

    public class KeyMappingService
    {
        private readonly string _configPath;
        private readonly Dictionary<Key, PlacementAction> _keyToAction = new Dictionary<Key, PlacementAction>();
        private readonly Dictionary<PlacementAction, Key> _actionToKey = new Dictionary<PlacementAction, Key>();

        public KeyMappingService()
        {
            _configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ME2LevelEditor",
                "keymappings.txt");
            LoadDefaults();
            Load();
        }

        private void LoadDefaults()
        {
            SetMapping(PlacementAction.PlaceZone, Key.A);
            SetMapping(PlacementAction.PlaceSolid, Key.S);
            SetMapping(PlacementAction.PlaceSectionLow, Key.F1);
            SetMapping(PlacementAction.PlaceSectionNormal, Key.F2);
            SetMapping(PlacementAction.PlaceSectionHigh, Key.F3);
            SetMapping(PlacementAction.PlaceSectionExtreme, Key.F4);
        }

        public void SetMapping(PlacementAction action, Key key)
        {
            var existingAction = _keyToAction.Where(kv => kv.Value == action).Select(kv => kv.Key).ToList();
            foreach (var k in existingAction)
                _keyToAction.Remove(k);

            if (_keyToAction.ContainsKey(key))
            {
                var displaced = _keyToAction[key];
                _actionToKey.Remove(displaced);
                _keyToAction.Remove(key);
            }

            _keyToAction[key] = action;
            _actionToKey[action] = key;
        }

        public PlacementAction GetAction(Key key)
        {
            return _keyToAction.TryGetValue(key, out var action) ? action : PlacementAction.None;
        }

        public Key GetKey(PlacementAction action)
        {
            return _actionToKey.TryGetValue(action, out var key) ? key : Key.None;
        }

        public List<KeyMappingEntry> GetAllMappings()
        {
            return new List<KeyMappingEntry>
            {
                new KeyMappingEntry { Action = PlacementAction.PlaceZone, KeyName = GetKey(PlacementAction.PlaceZone).ToString(), DisplayName = LocalizationService.T("KeyMap_Zone") },
                new KeyMappingEntry { Action = PlacementAction.PlaceSolid, KeyName = GetKey(PlacementAction.PlaceSolid).ToString(), DisplayName = LocalizationService.T("KeyMap_SolidHeld") },
                new KeyMappingEntry { Action = PlacementAction.PlaceSectionLow, KeyName = GetKey(PlacementAction.PlaceSectionLow).ToString(), DisplayName = LocalizationService.T("KeyMap_SectionLow") },
                new KeyMappingEntry { Action = PlacementAction.PlaceSectionNormal, KeyName = GetKey(PlacementAction.PlaceSectionNormal).ToString(), DisplayName = LocalizationService.T("KeyMap_SectionNormal") },
                new KeyMappingEntry { Action = PlacementAction.PlaceSectionHigh, KeyName = GetKey(PlacementAction.PlaceSectionHigh).ToString(), DisplayName = LocalizationService.T("KeyMap_SectionHigh") },
                new KeyMappingEntry { Action = PlacementAction.PlaceSectionExtreme, KeyName = GetKey(PlacementAction.PlaceSectionExtreme).ToString(), DisplayName = LocalizationService.T("KeyMap_SectionExtreme") },
            };
        }

        public void Save()
        {
            var dir = Path.GetDirectoryName(_configPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var lines = new List<string>();
            foreach (var kv in _actionToKey)
                lines.Add($"{kv.Key}={kv.Value}");

            File.WriteAllLines(_configPath, lines);
        }

        public void Load()
        {
            if (!File.Exists(_configPath)) return;

            foreach (var line in File.ReadAllLines(_configPath))
            {
                var parts = line.Split('=');
                if (parts.Length != 2) continue;

                if (Enum.TryParse<PlacementAction>(parts[0], out var action) &&
                    Enum.TryParse<Key>(parts[1], out var key))
                {
                    SetMapping(action, key);
                }
            }
        }
    }
}
