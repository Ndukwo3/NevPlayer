using System;
using System.Collections.Generic;
using NevPlayer.Core.Models;

namespace NevPlayer.Core.Services
{
    public interface IPlaybackService
    {
        IMediaPlayer Engine { get; }
        MediaItem? CurrentMedia { get; }
        PlaybackState State { get; }
        double Volume { get; set; }
        double PlaybackSpeed { get; set; }
        TimeSpan Position { get; }
        TimeSpan Duration { get; }
        IReadOnlyList<MediaItem> Queue { get; }

        event EventHandler? StateChanged;
        event EventHandler? PositionChanged;
        event EventHandler? MediaChanged;
        event EventHandler? QueueChanged;

        void Play();
        void Pause();
        void Stop();
        void Next();
        void Previous();
        void Seek(TimeSpan position);
        void Enqueue(MediaItem item);
        void RemoveFromQueue(int index);
        void MoveInQueue(int oldIndex, int newIndex);
        void PlayQueueItem(int index);
        void ClearQueue();

        // Subtitle Controls
        void LoadSubtitle(string filePath);
        void SetSubtitleVisibility(bool isVisible);
        void SetSubtitleDelay(double delayInSeconds);
        void CycleSubtitleTrack();

        // Audio Controls
        void CycleAudioTrack();
        void SetAudioDelay(double delayInSeconds);
    }
}
