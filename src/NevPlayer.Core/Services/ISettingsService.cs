namespace NevPlayer.Core.Services
{
    public interface ISettingsService
    {
        string AppTheme { get; set; }
        double DefaultPlaybackSpeed { get; set; }
        bool ResumePlayback { get; set; }
        bool HardwareAcceleration { get; set; }

        void Save();
        void Load();
    }
}
