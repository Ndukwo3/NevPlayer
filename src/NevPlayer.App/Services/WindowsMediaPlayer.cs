using System;
using System.Threading.Tasks;
using NevPlayer.Core.Models;
using NevPlayer.Core.Services;
using Windows.Media.Playback;
using Windows.Media.Core;
using Windows.Storage;

namespace NevPlayer.App.Services
{
    public class WindowsMediaPlayer : IMediaPlayer
    {
        private readonly MediaPlayer _player;
        private MediaPlaybackItem? _currentPlaybackItem;
        private double _subtitleDelayOffset = 0.0;

        public object? NativePlayer => _player;

        /// <summary>
        /// The current <see cref="MediaPlaybackItem"/> created during the last <see cref="Load"/> call.
        /// CinemaPage assigns this directly to <c>VideoSurface.Source</c> so the
        /// <see cref="Windows.UI.Xaml.Controls.MediaPlayerElement"/> owns the native player
        /// internally — no manual <c>SetMediaPlayer</c> calls are required.
        /// </summary>
        public Windows.Media.Playback.MediaPlaybackItem? CurrentPlaybackItem => _currentPlaybackItem;

        public event EventHandler<PlaybackState>? StateChanged;
        public event EventHandler<TimeSpan>? PositionChanged;
        public event EventHandler<TimeSpan>? DurationLoaded;

        private bool _isFullScreen;
        public bool IsFullScreen
        {
            get => _isFullScreen;
            set => _isFullScreen = value;
        }

        public TimeSpan Position => _player.PlaybackSession.Position;
        public TimeSpan Duration => _player.PlaybackSession.NaturalDuration;

        /// <summary>Raised when media fails to open, with an error message.</summary>
        public event EventHandler<string>? PlaybackFailed;

        /// <summary>Raised when current media reaches its end.</summary>
        public event EventHandler? MediaEnded;

        public WindowsMediaPlayer()
        {
            _player = new MediaPlayer();
            _player.AutoPlay = true;

            _player.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
            _player.PlaybackSession.PositionChanged     += PlaybackSession_PositionChanged;
            _player.MediaOpened                         += Player_MediaOpened;
            _player.MediaFailed                         += Player_MediaFailed;
            _player.MediaEnded                          += Player_MediaEnded;
        }

        // ── Event Handlers ────────────────────────────────────────────────────

        private void Player_MediaOpened(MediaPlayer sender, object args)
        {
            System.Diagnostics.Debug.WriteLine("[NevPlayer Diagnostics] Player_MediaOpened: Media successfully opened by native engine.");
            
            // Log video tracks count
            if (_currentPlaybackItem != null)
            {
                var trackCount = _currentPlaybackItem.VideoTracks.Count;
                System.Diagnostics.Debug.WriteLine($"[NevPlayer Diagnostics] Video Tracks Count: {trackCount}");
            }

            // Log natural dimensions
            System.Diagnostics.Debug.WriteLine($"[NevPlayer Diagnostics] Width: {sender.PlaybackSession.NaturalVideoWidth}");
            System.Diagnostics.Debug.WriteLine($"[NevPlayer Diagnostics] Height: {sender.PlaybackSession.NaturalVideoHeight}");

            // Duration becomes available once media is opened.
            // Do NOT fire StateChanged here — PlaybackSession_PlaybackStateChanged already handles it.
            DurationLoaded?.Invoke(this, sender.PlaybackSession.NaturalDuration);
        }

        private void Player_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            string message = args.ErrorMessage ?? args.Error.ToString();
            System.Diagnostics.Debug.WriteLine($"[NevPlayer Diagnostics] Player_MediaFailed: ERROR {args.Error} - {message}. ExtendedErrorCode: {args.ExtendedErrorCode?.Message}");
            StateChanged?.Invoke(this, PlaybackState.Idle);
            PlaybackFailed?.Invoke(this, $"Playback failed: {message}");
        }

        private void Player_MediaEnded(MediaPlayer sender, object args)
        {
            StateChanged?.Invoke(this, PlaybackState.Idle);
            MediaEnded?.Invoke(this, EventArgs.Empty);
        }

        private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            // To prevent log spam, we only log occasionally or omit, but since we need to verify position changes:
            // System.Diagnostics.Debug.WriteLine($"[NevPlayer Diagnostics] Position changed to: {sender.Position}");
            PositionChanged?.Invoke(this, sender.Position);
        }

        private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            System.Diagnostics.Debug.WriteLine($"[NevPlayer Diagnostics] PlaybackStateChanged: Native state is now {sender.PlaybackState}");
            PlaybackState state = sender.PlaybackState switch
            {
                MediaPlaybackState.Playing   => PlaybackState.Playing,
                MediaPlaybackState.Paused    => PlaybackState.Paused,
                MediaPlaybackState.Buffering => PlaybackState.Buffering,
                MediaPlaybackState.Opening   => PlaybackState.Buffering,
                _                            => PlaybackState.Idle
            };
            StateChanged?.Invoke(this, state);
        }

        // ── IMediaPlayer Implementation ────────────────────────────────────────

        // WMF does not use a DirectX swap chain — it attaches to MediaPlayerElement via SetMediaPlayer.
        // IsInitialized is always true; the swap chain methods are intentional no-ops.
        public bool IsInitialized => true;

        public void InitializeWithSwapChain(string[] swapChainOptions)
        {
            // No-op: WindowsMediaPlayer does not require swap chain initialisation.
        }

        public void ReleaseNativeResources()
        {
            // Stop playback. The underlying MediaPlayer is kept alive for the application lifetime.
            Stop();
        }

        /// <summary>
        /// Loads a local media file using StorageFile for maximum format compatibility.
        /// StorageFile bypasses URI security restrictions in unpackaged WinUI 3 apps and
        /// ensures Media Foundation can access all registered codecs (MP4, MKV, AVI,
        /// WebM, MOV, WMV, FLV, TS, M2TS, 3GP, MPEG, M4V, VOB, etc.).
        /// </summary>
        public void Load(string filePath)
        {
            // Fire-and-forget async load so the synchronous IMediaPlayer interface is preserved.
            // Errors are surfaced via the PlaybackFailed event.
            _ = LoadAsync(filePath);
        }

        private async Task LoadAsync(string filePath)
        {
            System.Diagnostics.Debug.WriteLine($"[NevPlayer Diagnostics] LoadAsync called for file: {filePath}");
            
            bool fileExists = System.IO.File.Exists(filePath);
            System.Diagnostics.Debug.WriteLine($"[NevPlayer Diagnostics] File exists check: {fileExists}");

            if (!fileExists)
            {
                System.Diagnostics.Debug.WriteLine("[NevPlayer Diagnostics] Aborting load because file does not exist.");
                PlaybackFailed?.Invoke(this, "File does not exist.");
                return;
            }

            try
            {
                // Signal UI that we are opening
                StateChanged?.Invoke(this, PlaybackState.Buffering);

                System.Diagnostics.Debug.WriteLine("[NevPlayer Diagnostics] Requesting StorageFile...");
                var storageFile = await StorageFile.GetFileFromPathAsync(filePath);
                System.Diagnostics.Debug.WriteLine("[NevPlayer Diagnostics] StorageFile acquired. Creating MediaSource...");
                
                var source      = MediaSource.CreateFromStorageFile(storageFile);
                _currentPlaybackItem = new MediaPlaybackItem(source);

                System.Diagnostics.Debug.WriteLine("[NevPlayer Diagnostics] Assigning MediaPlaybackItem to _player.Source...");
                _player.Source = _currentPlaybackItem;
                System.Diagnostics.Debug.WriteLine("[NevPlayer Diagnostics] Source assigned successfully.");
                // AutoPlay = true — player starts automatically once MediaOpened fires.
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NevPlayer Diagnostics] Exception during LoadAsync: {ex}");
                StateChanged?.Invoke(this, PlaybackState.Idle);
                PlaybackFailed?.Invoke(this, $"Cannot open file: {ex.Message}");
            }
        }

        public void Play()
        {
            _player.Play();
        }

        public void Pause()
        {
            _player.Pause();
        }

        public void Stop()
        {
            _player.Pause();
            try { _player.PlaybackSession.Position = TimeSpan.Zero; } catch { /* ignore if no source */ }
        }

        public void Seek(TimeSpan position)
        {
            try { _player.PlaybackSession.Position = position; } catch { }
        }

        public void SetVolume(double volume)
        {
            // Clamp natively to 1.0 for now, MPV implementation will support true amplification up to 2.0
            _player.Volume = Math.Clamp(volume / 100.0, 0.0, 1.0);
        }

        // ── Subtitle / Audio Implementation ────────────────────────────────────

        public void LoadSubtitle(string filePath)
        {
            if (_currentPlaybackItem == null || string.IsNullOrWhiteSpace(filePath)) return;

            try
            {
                var uri = new Uri("file:///" + filePath.Replace("\\", "/"));
                var source = TimedTextSource.CreateFromUri(uri);
                
                // Track source loading failures
                source.Resolved += (s, args) =>
                {
                    if (args.Error != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[NevPlayer Diagnostics] Subtitle source error: {args.Error.ExtendedError?.Message}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[NevPlayer Diagnostics] External Subtitle Track Resolved Successfully.");
                        // Enable the newly resolved track
                        DispatcherQueueSelector();
                    }
                };

                _currentPlaybackItem.Source.ExternalTimedTextSources.Add(source);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NevPlayer Diagnostics] Error loading external subtitle: {ex.Message}");
            }
        }

        private void DispatcherQueueSelector()
        {
            // Auto-enable first resolved external track on UWP main thread
            var tracks = _currentPlaybackItem?.TimedMetadataTracks;
            if (tracks != null && tracks.Count > 0)
            {
                // Set the last track (which is the newly added one) as active
                _currentPlaybackItem!.TimedMetadataTracks.SetPresentationMode((uint)(tracks.Count - 1), TimedMetadataTrackPresentationMode.PlatformPresented);
            }
        }

        public void SetSubtitleVisibility(bool isVisible)
        {
            if (_currentPlaybackItem == null) return;

            var tracks = _currentPlaybackItem.TimedMetadataTracks;
            for (uint i = 0; i < tracks.Count; i++)
            {
                var track = tracks[(int)i];
                if (track.TimedMetadataKind == TimedMetadataKind.Subtitle || track.TimedMetadataKind == TimedMetadataKind.ImageSubtitle)
                {
                    var mode = isVisible ? TimedMetadataTrackPresentationMode.PlatformPresented : TimedMetadataTrackPresentationMode.Disabled;
                    tracks.SetPresentationMode(i, mode);
                }
            }
        }

        public void SetSubtitleDelay(double delayInSeconds)
        {
            if (_currentPlaybackItem == null) return;

            // Calculate the change in delay (delta) since the last call
            double delta = delayInSeconds - _subtitleDelayOffset;
            if (delta == 0) return;

            _subtitleDelayOffset = delayInSeconds;

            var tracks = _currentPlaybackItem.TimedMetadataTracks;
            for (int i = 0; i < tracks.Count; i++)
            {
                var track = tracks[i];
                if (track.TimedMetadataKind == TimedMetadataKind.Subtitle || track.TimedMetadataKind == TimedMetadataKind.ImageSubtitle)
                {
                    // Only apply offset shifts to active TimedMetadataTracks
                    var mode = tracks.GetPresentationMode((uint)i);
                    if (mode == TimedMetadataTrackPresentationMode.PlatformPresented)
                    {
                        var timeDelta = TimeSpan.FromSeconds(delta);
                        foreach (var cue in track.Cues)
                        {
                            try
                            {
                                cue.StartTime += timeDelta;
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[NevPlayer Diagnostics] Failed to shift cue: {ex.Message}");
                            }
                        }
                        System.Diagnostics.Debug.WriteLine($"[NevPlayer Diagnostics] Shifted subtitle cues by {delta}s. Total delay: {_subtitleDelayOffset}s");
                    }
                }
            }
        }

        public void CycleSubtitleTrack()
        {
            if (_currentPlaybackItem == null) return;

            var tracks = _currentPlaybackItem.TimedMetadataTracks;
            int activeIndex = -1;

            // Find current active track
            for (int i = 0; i < tracks.Count; i++)
            {
                var track = tracks[i];
                if (track.TimedMetadataKind == TimedMetadataKind.Subtitle || track.TimedMetadataKind == TimedMetadataKind.ImageSubtitle)
                {
                    var mode = tracks.GetPresentationMode((uint)i);
                    if (mode == TimedMetadataTrackPresentationMode.PlatformPresented)
                    {
                        activeIndex = i;
                        break;
                    }
                }
            }

            // Cycle active index
            int nextIndex = activeIndex + 1;
            
            // Disable previous track
            if (activeIndex != -1)
            {
                tracks.SetPresentationMode((uint)activeIndex, TimedMetadataTrackPresentationMode.Disabled);
            }

            // Find next valid subtitle track
            while (nextIndex < tracks.Count)
            {
                var track = tracks[nextIndex];
                if (track.TimedMetadataKind == TimedMetadataKind.Subtitle || track.TimedMetadataKind == TimedMetadataKind.ImageSubtitle)
                {
                    tracks.SetPresentationMode((uint)nextIndex, TimedMetadataTrackPresentationMode.PlatformPresented);
                    return; // Switched!
                }
                nextIndex++;
            }

            // If we ran past the end, disable all (off mode)
        }

        public void CycleAudioTrack()
        {
            if (_currentPlaybackItem == null) return;

            var tracks = _currentPlaybackItem.AudioTracks;
            if (tracks.Count <= 1) return;

            int currentIndex = tracks.SelectedIndex;
            int nextIndex = (currentIndex + 1) % tracks.Count;
            tracks.SelectedIndex = nextIndex;
        }

        public void SetAudioDelay(double delayInSeconds)
        {
            // WindowsMediaPlayer does not support native audio stream timing shifting offset.
            System.Diagnostics.Debug.WriteLine($"[NevPlayer Diagnostics] Audio delay offset set to: {delayInSeconds}s (not natively supported by MediaPlayer)");
        }

        public void SetPlaybackRate(double rate)
        {
            _player.PlaybackSession.PlaybackRate = rate;
        }

        public void Dispose()
        {
            _player.Dispose();
        }

        public System.Collections.Generic.IReadOnlyList<MediaTrackInfo> GetSubtitleTracks()
        {
            var list = new System.Collections.Generic.List<MediaTrackInfo>();
            if (_currentPlaybackItem != null)
            {
                var tracks = _currentPlaybackItem.TimedMetadataTracks;
                int activeIndex = GetActiveSubtitleTrackIndex();
                int trackIdx = 0;
                for (int i = 0; i < tracks.Count; i++)
                {
                    var track = tracks[i];
                    if (track.TimedMetadataKind == TimedMetadataKind.Subtitle || track.TimedMetadataKind == TimedMetadataKind.ImageSubtitle)
                    {
                        var lang = string.IsNullOrWhiteSpace(track.Language) ? "Unknown" : track.Language;
                        var label = string.IsNullOrWhiteSpace(track.Label) ? $"Track {trackIdx + 1}" : track.Label;
                        list.Add(new MediaTrackInfo
                        {
                            Index = trackIdx,
                            Language = lang,
                            Name = label,
                            IsActive = trackIdx == activeIndex
                        });
                        trackIdx++;
                    }
                }
            }
            return list;
        }

        public int GetActiveSubtitleTrackIndex()
        {
            if (_currentPlaybackItem != null)
            {
                var tracks = _currentPlaybackItem.TimedMetadataTracks;
                int subIdx = 0;
                for (int i = 0; i < tracks.Count; i++)
                {
                    var track = tracks[i];
                    if (track.TimedMetadataKind == TimedMetadataKind.Subtitle || track.TimedMetadataKind == TimedMetadataKind.ImageSubtitle)
                    {
                        var mode = tracks.GetPresentationMode((uint)i);
                        if (mode == TimedMetadataTrackPresentationMode.PlatformPresented || mode == TimedMetadataTrackPresentationMode.Hidden)
                        {
                            return subIdx;
                        }
                        subIdx++;
                    }
                }
            }
            return -1;
        }

        public void SetSubtitleTrack(int index)
        {
            if (_currentPlaybackItem != null)
            {
                var tracks = _currentPlaybackItem.TimedMetadataTracks;
                int subIdx = 0;
                for (int i = 0; i < tracks.Count; i++)
                {
                    var track = tracks[i];
                    if (track.TimedMetadataKind == TimedMetadataKind.Subtitle || track.TimedMetadataKind == TimedMetadataKind.ImageSubtitle)
                    {
                        if (subIdx == index)
                        {
                            tracks.SetPresentationMode((uint)i, TimedMetadataTrackPresentationMode.PlatformPresented);
                        }
                        else
                        {
                            tracks.SetPresentationMode((uint)i, TimedMetadataTrackPresentationMode.Disabled);
                        }
                        subIdx++;
                    }
                }
            }
        }

        public System.Collections.Generic.IReadOnlyList<MediaTrackInfo> GetAudioTracks()
        {
            var list = new System.Collections.Generic.List<MediaTrackInfo>();
            if (_currentPlaybackItem != null)
            {
                var tracks = _currentPlaybackItem.AudioTracks;
                int activeIndex = GetActiveAudioTrackIndex();
                for (int i = 0; i < tracks.Count; i++)
                {
                    var track = tracks[i];
                    var lang = string.IsNullOrWhiteSpace(track.Language) ? "Unknown" : track.Language;
                    var label = string.IsNullOrWhiteSpace(track.Label) ? $"Track {i + 1}" : track.Label;
                    list.Add(new MediaTrackInfo
                    {
                        Index = i,
                        Language = lang,
                        Name = label,
                        IsActive = i == activeIndex
                    });
                }
            }
            return list;
        }

        public int GetActiveAudioTrackIndex()
        {
            if (_currentPlaybackItem != null)
            {
                return _currentPlaybackItem.AudioTracks.SelectedIndex;
            }
            return -1;
        }

        public void SetAudioTrack(int index)
        {
            if (_currentPlaybackItem != null && index >= 0 && index < _currentPlaybackItem.AudioTracks.Count)
            {
                _currentPlaybackItem.AudioTracks.SelectedIndex = index;
            }
        }
    }
}
