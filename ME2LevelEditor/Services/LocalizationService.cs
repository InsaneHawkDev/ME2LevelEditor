using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace ME2LevelEditor.Services
{
    public static class LocalizationService
    {
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ME2LevelEditor",
            "language.txt");

        private static ResourceDictionary _currentDictionary;
        private static string _currentLang = "en";

        public static readonly List<LanguageInfo> AvailableLanguages = new List<LanguageInfo>
        {
            new LanguageInfo("en", "English"),
            new LanguageInfo("fr", "Français"),
            new LanguageInfo("it", "Italiano"),
            new LanguageInfo("es", "Español"),
        };

        public static string CurrentLanguage => _currentLang;

        public static void Init()
        {
            var saved = LoadSavedLanguage();
            SetLanguage(saved ?? "en");
        }

        public static void SetLanguage(string langCode)
        {
            var uri = new Uri($"Resources/Langs/Strings.{langCode}.xaml", UriKind.Relative);
            ResourceDictionary dict;
            try
            {
                dict = new ResourceDictionary { Source = uri };
            }
            catch
            {
                if (langCode != "en")
                {
                    SetLanguage("en");
                    return;
                }
                throw;
            }

            var merged = Application.Current.Resources.MergedDictionaries;
            if (_currentDictionary != null)
                merged.Remove(_currentDictionary);

            merged.Add(dict);
            _currentDictionary = dict;
            _currentLang = langCode;

            SaveLanguage(langCode);
        }

        public static string T(string key)
        {
            return Application.Current.TryFindResource(key) as string ?? $"[{key}]";
        }

        public static string T(string key, params object[] args)
        {
            var template = T(key);
            try
            {
                return string.Format(template, args);
            }
            catch
            {
                return template;
            }
        }

        private static string LoadSavedLanguage()
        {
            try
            {
                if (File.Exists(ConfigPath))
                    return File.ReadAllText(ConfigPath).Trim();
            }
            catch { }
            return null;
        }

        private static void SaveLanguage(string langCode)
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(ConfigPath, langCode);
            }
            catch { }
        }
    }

    public class LanguageInfo
    {
        public string Code { get; }
        public string DisplayName { get; }

        public LanguageInfo(string code, string displayName)
        {
            Code = code;
            DisplayName = displayName;
        }
    }
}
