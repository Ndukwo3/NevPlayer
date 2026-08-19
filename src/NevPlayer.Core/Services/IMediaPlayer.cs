using System;
using NevPlayer.Core.Models;

namespace NevPlayer.Core.Services
{
    public interface IMediaPlayer : IDisposable
    {
        object? NativePlayer { get; }
        bool IsInitialized { get; }
        
        void InitializeWithSwapChain(string[] arguments);
        void ReleaseNativeResources();
        void Load(string filePath);
        void Play();
        void Pause();
        void Stop();
        void Seek(TimeSpan position);
        void SetVolume(double volume);
        
        // Subtitle Controls
        void LoadSubtitle(string filePath);
        void SetSubtitleVisibility(bool isVisible);
        void SetSubtitleDelay(double delayInSeconds);
        void CycleSubtitleTrack();
        
        // Audio Controls
        void CycleAudioTrack();
        void SetAudioDelay(double delayInSeconds);
        
        // Speed Control
        void SetPlaybackRate(double rate);
        
        // Playback Info
        TimeSpan Position { get; }
        TimeSpan Duration { get; }
        
        // Fullscreen Support
        bool IsFullScreen { get; set; }
        
        // Extended Track Info APIs
        System.Collections.Generic.IReadOnlyList<MediaTrackInfo> GetSubtitleTracks();
        int GetActiveSubtitleTrackIndex();
        void SetSubtitleTrack(int index);
        
        System.Collections.Generic.IReadOnlyList<MediaTrackInfo> GetAudioTracks();
        int GetActiveAudioTrackIndex();
        void SetAudioTrack(int index);

        event EventHandler<PlaybackState>? StateChanged;
        public event EventHandler<TimeSpan>? PositionChanged;
        public event EventHandler<TimeSpan>? DurationLoaded;
    }
}
