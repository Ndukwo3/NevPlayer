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

        /// <summary>
        /// Raised after <see cref="AttachVideoSurface"/> successfully initialises the
        /// underlying VLC engine. The event argument is the native
        /// <c>LibVLCSharp.Shared.MediaPlayer</c> instance (typed as <c>object?</c> to avoid
        /// leaking LibVLC types into the Core layer). CinemaPage casts and assigns it to
        /// <c>VlcVideoSurface.MediaPlayer</c>.
        /// </summary>
        event EventHandler<object?>? VideoSurfaceAttached;

        void Play();
        void Pause();
        void Stop();
        void Next();
        void Previous();
        void Seek(TimeSpan position);
        void Enqueue(MediaItem item, bool autoPlay = true);
        void LoadCurrent();
        void RemoveFromQueue(int index);
        void MoveInQueue(int oldIndex, int newIndex);
        void PlayQueueItem(int index);
        void ClearQueue();

        // Video Surface Lifecycle
        /// <summary>
        /// Called from <c>VideoView.Initialized</c>. Passes swap chain options into the
        /// VLC engine so <c>LibVLC</c> can be constructed with the correct Direct3D
        /// surface context. Also triggers <see cref="LoadCurrent"/> and <see cref="Play"/>
        /// once the engine is ready.
        /// </summary>
        void AttachVideoSurface(string[] swapChainOptions);

        /// <summary>
        /// Stops playback and releases native LibVLC resources tied to the current
        /// VideoView surface. Should be called from <c>CinemaPage.Unloaded</c> when a
        /// full teardown is desired.
        /// </summary>
        void DetachVideoSurface();

        /// <summary>
        /// True once <see cref="AttachVideoSurface"/> has completed for the current
        /// VideoView. False after <see cref="DetachVideoSurface"/>.
        /// </summary>
        bool IsVideoSurfaceReady { get; }

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
