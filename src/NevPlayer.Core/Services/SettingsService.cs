using System;
using System.IO;
using System.Text.Json;

namespace NevPlayer.Core.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly string _settingsFilePath;
        private SettingsData _data = new SettingsData();

        public SettingsService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var settingsDir = Path.Combine(appDataPath, "NevPlayer");

            if (!Directory.Exists(settingsDir))
                Directory.CreateDirectory(settingsDir);

            _settingsFilePath = Path.Combine(settingsDir, "settings.json");
            Load();
        }

        public string AppTheme
        {
            get => _data.AppTheme;
            set { _data.AppTheme = value; Save(); }
        }

        public double DefaultPlaybackSpeed
        {
            get => _data.DefaultPlaybackSpeed;
            set { _data.DefaultPlaybackSpeed = value; Save(); }
        }

        public bool ResumePlayback
        {
            get => _data.ResumePlayback;
            set { _data.ResumePlayback = value; Save(); }
        }

        public bool HardwareAcceleration
        {
            get => _data.HardwareAcceleration;
            set { _data.HardwareAcceleration = value; Save(); }
        }

        public void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsFilePath, json);
            }
            catch
            {
                // Ignore save failures silently
            }
        }

        public void Load()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    _data = JsonSerializer.Deserialize<SettingsData>(json) ?? new SettingsData();
                }
            }
            catch
            {
                _data = new SettingsData(); // Reset to defaults on failure
            }
        }

        private class SettingsData
        {
            public string AppTheme { get; set; } = "Dark";
            public double DefaultPlaybackSpeed { get; set; } = 1.0;
            public bool ResumePlayback { get; set; } = true;
            public bool HardwareAcceleration { get; set; } = true;
        }
    }
}
