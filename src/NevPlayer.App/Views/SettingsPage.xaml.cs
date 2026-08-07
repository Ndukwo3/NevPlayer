using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NevPlayer.Core.Services;

namespace NevPlayer.App.Views
{
    public sealed partial class SettingsPage : Page
    {
        private readonly ISettingsService? _settingsService;
        private bool _isLoading = true;

        public SettingsPage()
        {
            this.InitializeComponent();
            _settingsService = App.SettingsService;
            Loaded += SettingsPage_Loaded;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoading = true;
            DarkModeToggle.IsOn = _settingsService?.AppTheme != "Light";
            ResumeToggle.IsOn = _settingsService?.ResumePlayback ?? true;
            HardwareAccelToggle.IsOn = _settingsService?.HardwareAcceleration ?? true;
            LibVlcToggle.IsOn = _settingsService?.UseLibVLC ?? false;
            _isLoading = false;
        }

        private void DarkModeToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isLoading || _settingsService == null) return;

            var theme = DarkModeToggle.IsOn ? "Dark" : "Light";
            _settingsService.AppTheme = theme;

            if (App.Current is App app)
            {
                app.ApplyCurrentTheme();
            }
        }

        private void ResumeToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isLoading || _settingsService == null) return;
            _settingsService.ResumePlayback = ResumeToggle.IsOn;
        }

        private void HardwareAccelToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isLoading || _settingsService == null) return;
            _settingsService.HardwareAcceleration = HardwareAccelToggle.IsOn;
        }

        private void LibVlcToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isLoading || _settingsService == null) return;
            _settingsService.UseLibVLC = LibVlcToggle.IsOn;
        }
    }
}
