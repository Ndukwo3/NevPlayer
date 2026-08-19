using System;
using NevPlayer.Core.Models;
using NevPlayer.Core.Services;

namespace NevPlayer.App.Services
{
    public class SwitchableMediaPlayer : IMediaPlayer
    {
        private WindowsMediaPlayer? _wmfEngine;
        private VlcMediaPlayer? _vlcEngine;
        private readonly ISettingsService _settingsService;
        private string? _currentFilePath;
        private double _currentVolume = 100;
        private double _currentPlaybackRate = 1.0;
        private PlaybackState _lastState = PlaybackState.Idle;

        public event EventHandler<PlaybackState>? StateChanged;
        public event EventHandler<TimeSpan>? PositionChanged;
        public event EventHandler<TimeSpan>? DurationLoaded;
        public event EventHandler? EngineChanged;

        public WindowsMediaPlayer WmfEngine
        {
            get
            {
                if (_wmfEngine == null)
                {
                    _wmfEngine = new WindowsMediaPlayer();
                    _wmfEngine.StateChanged += (s, state) =>
                    {
                        if (!_settingsService.UseLibVLC)
                        {
                            _lastState = state;
                            StateChanged?.Invoke(this, state);
                        }
                    };
                    _wmfEngine.PositionChanged += (s, pos) =>
                    {
                        if (!_settingsService.UseLibVLC) PositionChanged?.Invoke(this, pos);
                    };
                    _wmfEngine.DurationLoaded += (s, dur) =>
                    {
                        if (!_settingsService.UseLibVLC) DurationLoaded?.Invoke(this, dur);
                    };
                }
                return _wmfEngine;
            }
        }

        public VlcMediaPlayer VlcEngine
        {
            get
            {
                if (_vlcEngine == null)
                {
                    _vlcEngine = new VlcMediaPlayer();
                    _vlcEngine.StateChanged += (s, state) =>
                    {
                        if (_settingsService.UseLibVLC)
                        {
                            _lastState = state;
                            StateChanged?.Invoke(this, state);
                        }
                    };
                    _vlcEngine.PositionChanged += (s, pos) =>
                    {
                        if (_settingsService.UseLibVLC) PositionChanged?.Invoke(this, pos);
                    };
                    _vlcEngine.DurationLoaded += (s, dur) =>
                    {
                        if (_settingsService.UseLibVLC) DurationLoaded?.Invoke(this, dur);
                    };
                }
                return _vlcEngine;
            }
        }

        public IMediaPlayer ActiveEngine => _settingsService.UseLibVLC ? (IMediaPlayer)VlcEngine : (IMediaPlayer)WmfEngine;

        public object? NativePlayer => ActiveEngine.NativePlayer;
        public object? VlcNativePlayer => VlcEngine.NativePlayer;
        public object? WmfNativePlayer => WmfEngine.NativePlayer;

        public TimeSpan Position => ActiveEngine.Position;
        public TimeSpan Duration => ActiveEngine.Duration;

        public bool IsFullScreen
        {
            get => ActiveEngine.IsFullScreen;
            set => ActiveEngine.IsFullScreen = value;
        }

        public bool IsInitialized => ActiveEngine.IsInitialized;

        public SwitchableMediaPlayer(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _settingsService.UseLibVLCChanged += SettingsService_UseLibVLCChanged;
        }

        private void SettingsService_UseLibVLCChanged(object? sender, bool useLibVLC)
        {
            SwitchEngine(useLibVLC);
        }

        public void SwitchEngine(bool useLibVLC)
        {
            try
            {
                var wasPlaying = _lastState == PlaybackState.Playing;
                var currentPos = Position;
                var currentVol = _currentVolume;
                var currentRate = _currentPlaybackRate;
                var filePath = _currentFilePath;

                // Stop the former active engine
                if (useLibVLC)
                {
                    _wmfEngine?.Stop();
                }
                else
                {
                    _vlcEngine?.Stop();
                }

                // If media was loaded, load into the newly active engine
                if (!string.IsNullOrEmpty(filePath))
                {
                    IMediaPlayer newEngine = useLibVLC ? (IMediaPlayer)VlcEngine : (IMediaPlayer)WmfEngine;
                    newEngine.Load(filePath);
                    newEngine.SetVolume(currentVol);
                    newEngine.SetPlaybackRate(currentRate);

                    if (currentPos > TimeSpan.Zero)
                    {
                        newEngine.Seek(currentPos);
                    }

                    if (wasPlaying)
                    {
                        newEngine.Play();
                    }
                }

                EngineChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SwitchableMediaPlayer Switch Error] {ex.Message}");
            }
        }

        public void InitializeWithSwapChain(string[] swapChainOptions)
        {
            if (_settingsService.UseLibVLC)
            {
                VlcEngine.InitializeWithSwapChain(swapChainOptions);
            }
            else
            {
                WmfEngine.InitializeWithSwapChain(swapChainOptions);
            }
        }

        public void ReleaseNativeResources()
        {
            if (_settingsService.UseLibVLC)
            {
                VlcEngine.ReleaseNativeResources();
            }
            else
            {
                WmfEngine.ReleaseNativeResources();
            }
        }

        public void Load(string filePath)
        {
            _currentFilePath = filePath;

            if (_settingsService.UseLibVLC)
            {
                _wmfEngine?.Stop();
                VlcEngine.Load(filePath);
            }
            else
            {
                _vlcEngine?.Stop();
                WmfEngine.Load(filePath);
            }
        }

        public void Play() => ActiveEngine.Play();
        public void Pause() => ActiveEngine.Pause();
        public void Stop() => ActiveEngine.Stop();
        public void Seek(TimeSpan position) => ActiveEngine.Seek(position);
        
        public void SetVolume(double volume)
        {
            _currentVolume = volume;
            ActiveEngine.SetVolume(volume);
        }

        public void SetPlaybackRate(double rate)
        {
            _currentPlaybackRate = rate;
            ActiveEngine.SetPlaybackRate(rate);
        }

        public void LoadSubtitle(string filePath) => ActiveEngine.LoadSubtitle(filePath);
        public void SetSubtitleVisibility(bool isVisible) => ActiveEngine.SetSubtitleVisibility(isVisible);
        public void SetSubtitleDelay(double delayInSeconds) => ActiveEngine.SetSubtitleDelay(delayInSeconds);
        public void CycleSubtitleTrack() => ActiveEngine.CycleSubtitleTrack();

        public void CycleAudioTrack() => ActiveEngine.CycleAudioTrack();
        public void SetAudioDelay(double delayInSeconds) => ActiveEngine.SetAudioDelay(delayInSeconds);

        public System.Collections.Generic.IReadOnlyList<MediaTrackInfo> GetSubtitleTracks() => ActiveEngine.GetSubtitleTracks();
        public int GetActiveSubtitleTrackIndex() => ActiveEngine.GetActiveSubtitleTrackIndex();
        public void SetSubtitleTrack(int index) => ActiveEngine.SetSubtitleTrack(index);

        public System.Collections.Generic.IReadOnlyList<MediaTrackInfo> GetAudioTracks() => ActiveEngine.GetAudioTracks();
        public int GetActiveAudioTrackIndex() => ActiveEngine.GetActiveAudioTrackIndex();
        public void SetAudioTrack(int index) => ActiveEngine.SetAudioTrack(index);

        public void Dispose()
        {
            if (_settingsService != null)
            {
                _settingsService.UseLibVLCChanged -= SettingsService_UseLibVLCChanged;
            }
            _wmfEngine?.Dispose();
            _vlcEngine?.Dispose();
        }
    }
}
