using System;
using NevPlayer.Core.Models;

namespace NevPlayer.Core.Services
{
    public interface IMediaPlayer : IDisposable
    {
        object? NativePlayer { get; }
        
        void Initialize(IntPtr windowHandle);
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
        
        event EventHandler<PlaybackState>? StateChanged;
        event EventHandler<TimeSpan>? PositionChanged;
        event EventHandler<TimeSpan>? DurationLoaded;
    }
}
