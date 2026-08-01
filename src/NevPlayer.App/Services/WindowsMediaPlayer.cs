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

        public object? NativePlayer => _player;

        public event EventHandler<PlaybackState>? StateChanged;
        public event EventHandler<TimeSpan>? PositionChanged;
        public event EventHandler<TimeSpan>? DurationLoaded;

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

        public void Initialize(IntPtr windowHandle)
        {
            // Not needed for MediaPlayerElement integration
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
                var playbackItem = new MediaPlaybackItem(source);

                System.Diagnostics.Debug.WriteLine("[NevPlayer Diagnostics] Assigning MediaPlaybackItem to _player.Source...");
                _player.Source = playbackItem;
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
            _player.Volume = Math.Clamp(volume / 100.0, 0.0, 1.0);
        }

        // ── Subtitle / Audio Stubs ─────────────────────────────────────────────
        // The Windows MediaPlayer API does not expose low-level track/delay control.
        // These will be fully implemented when the libmpv backend is integrated.

        public void LoadSubtitle(string filePath)
        {
            // TODO: TimedTextSource for external .srt/.vtt subtitles
        }

        public void SetSubtitleVisibility(bool isVisible)  { }
        public void SetSubtitleDelay(double delayInSeconds) { }
        public void CycleSubtitleTrack()                   { }
        public void CycleAudioTrack()                      { }
        public void SetAudioDelay(double delayInSeconds)    { }

        public void SetPlaybackRate(double rate)
        {
            _player.PlaybackSession.PlaybackRate = rate;
        }

        public void Dispose()
        {
            _player.Dispose();
        }
    }
}
