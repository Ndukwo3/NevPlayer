using System;
using System.IO;
using LibVLCSharp.Shared;
using NevPlayer.Core.Models;
using NevPlayer.Core.Services;

namespace NevPlayer.App.Services
{
    public class VlcMediaPlayer : IMediaPlayer
    {
        // LibVLC and MediaPlayer are NOT created at construction time.
        // They are created lazily inside InitializeWithSwapChain(), which must be called
        // from within the VideoView.Initialized event handler so that the Direct3D 11
        // swap chain context is available for the native renderer.
        private LibVLC? _libVLC;
        private MediaPlayer? _mediaPlayer;

        private string? _currentFilePath;
        private double _currentVolume = 100;
        private double _currentPlaybackRate = 1.0;
        private bool _isPlaying = false;
        private bool _isInitialized = false;
        private TimeSpan _lastPosition = TimeSpan.Zero;

        public object? NativePlayer => _mediaPlayer;

        /// <summary>
        /// Returns true once <see cref="InitializeWithSwapChain"/> has completed.
        /// Guards on Load/Play/Pause/Stop prevent engine calls before init.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        public event EventHandler<PlaybackState>? StateChanged;
        public event EventHandler<TimeSpan>? PositionChanged;
        public event EventHandler<TimeSpan>? DurationLoaded;
        public event EventHandler? MediaEnded;
        public event EventHandler<string>? PlaybackFailed;

        private bool _isFullScreen;
        public bool IsFullScreen
        {
            get => _isFullScreen;
            set
            {
                _isFullScreen = value;
                if (_mediaPlayer != null)
                {
                    _mediaPlayer.Fullscreen = value;
                }
            }
        }

        public TimeSpan Position => TimeSpan.FromMilliseconds(_mediaPlayer?.Time ?? 0);
        public TimeSpan Duration => TimeSpan.FromMilliseconds(_mediaPlayer?.Length ?? 0);


        public VlcMediaPlayer()
        {
            // Intentionally empty. LibVLC and MediaPlayer are created in
            // InitializeWithSwapChain() once the VideoView surface is ready.
        }

        /// <summary>
        /// Creates <see cref="LibVLC"/> with the WinUI VideoView swap chain options and
        /// then creates a bound <see cref="MediaPlayer"/>. Must be called from the
        /// <c>VideoView.Initialized</c> event handler.
        /// If already initialised, the existing resources are cleanly released first so
        /// that re-navigation to CinemaPage always gets a fresh surface binding.
        /// </summary>
        public void InitializeWithSwapChain(string[] swapChainOptions)
        {
            // Release any existing resources before re-initialising
            if (_isInitialized)
            {
                ReleaseNativeResources();
            }

            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string[] candidatePaths = new[]
                {
                    System.IO.Path.Combine(baseDir, "libvlc", "win-x64"),
                    System.IO.Path.Combine(baseDir, "libvlc", "win-x86"),
                    System.IO.Path.Combine(baseDir, "runtimes", "win-x64", "native"),
                    baseDir
                };

                bool coreInitialized = false;
                foreach (var path in candidatePaths)
                {
                    if (File.Exists(System.IO.Path.Combine(path, "libvlc.dll")))
                    {
                        LibVLCSharp.Shared.Core.Initialize(path);
                        coreInitialized = true;
                        break;
                    }
                }
                if (!coreInitialized)
                {
                    LibVLCSharp.Shared.Core.Initialize();
                }

                // Merge swap chain options with our standard options
                var extraOptions = new[] { "--no-video-title-show", "--no-sub-autodetect-file" };
                var allOptions = new string[swapChainOptions.Length + extraOptions.Length];
                swapChainOptions.CopyTo(allOptions, 0);
                extraOptions.CopyTo(allOptions, swapChainOptions.Length);

                _libVLC = new LibVLC(allOptions);
                _mediaPlayer = new MediaPlayer(_libVLC);

                // Restore previously configured volume and speed
                _mediaPlayer.Volume = (int)Math.Clamp(_currentVolume, 0, 200);
                _mediaPlayer.SetRate((float)_currentPlaybackRate);

                HookMediaPlayerEvents(_mediaPlayer);
                _isInitialized = true;

                System.Diagnostics.Debug.WriteLine("[VlcMediaPlayer] InitializeWithSwapChain complete. LibVLC and MediaPlayer created.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VlcMediaPlayer] InitializeWithSwapChain failed: {ex.Message}");
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Stops playback and disposes the <see cref="MediaPlayer"/> and <see cref="LibVLC"/>
        /// instances created by <see cref="InitializeWithSwapChain"/>. Safe to call even
        /// when not initialised.
        /// </summary>
        public void ReleaseNativeResources()
        {
            _isInitialized = false;
            _isPlaying = false;

            try { _mediaPlayer?.Stop(); } catch { }

            if (_mediaPlayer != null)
            {
                _mediaPlayer.TimeChanged -= MediaPlayer_TimeChanged;
                _mediaPlayer.LengthChanged -= MediaPlayer_LengthChanged;
                try { _mediaPlayer.Dispose(); } catch { }
                _mediaPlayer = null;
            }

            if (_libVLC != null)
            {
                try { _libVLC.Dispose(); } catch { }
                _libVLC = null;
            }

            System.Diagnostics.Debug.WriteLine("[VlcMediaPlayer] ReleaseNativeResources complete.");
        }

        private void HookMediaPlayerEvents(MediaPlayer player)
        {
            player.TimeChanged += MediaPlayer_TimeChanged;
            player.LengthChanged += MediaPlayer_LengthChanged;
            
            player.Playing += (s, e) =>
            {
                _isPlaying = true;
                StateChanged?.Invoke(this, PlaybackState.Playing);
            };
            player.Paused += (s, e) =>
            {
                _isPlaying = false;
                StateChanged?.Invoke(this, PlaybackState.Paused);
            };
            player.Stopped += (s, e) =>
            {
                _isPlaying = false;
                StateChanged?.Invoke(this, PlaybackState.Idle);
            };
            player.Buffering += (s, e) =>
            {
                if (e.Cache < 100)
                {
                    StateChanged?.Invoke(this, PlaybackState.Buffering);
                }
                else
                {
                    StateChanged?.Invoke(this, _isPlaying ? PlaybackState.Playing : PlaybackState.Idle);
                }
            };
            player.Opening += (s, e) => StateChanged?.Invoke(this, PlaybackState.Buffering);

            player.EndReached += (s, e) =>
            {
                _isPlaying = false;
                StateChanged?.Invoke(this, PlaybackState.Idle);
                MediaEnded?.Invoke(this, EventArgs.Empty);
            };

            player.EncounteredError += (s, e) =>
            {
                _isPlaying = false;
                StateChanged?.Invoke(this, PlaybackState.Idle);
                PlaybackFailed?.Invoke(this, "LibVLC playback error encountered.");
            };
        }

        private void MediaPlayer_LengthChanged(object? sender, MediaPlayerLengthChangedEventArgs e)
        {
            if (e.Length > 0)
            {
                DurationLoaded?.Invoke(this, TimeSpan.FromMilliseconds(e.Length));
            }
        }

        private void MediaPlayer_TimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
        {
            if (_mediaPlayer == null) return;
            var positionMs = e.Time;
            if (positionMs >= 0)
            {
                _lastPosition = TimeSpan.FromMilliseconds(positionMs);
                PositionChanged?.Invoke(this, _lastPosition);
            }
        }

        public void Load(string filePath)
        {
            if (!_isInitialized || _mediaPlayer == null) return;
            if (!File.Exists(filePath)) return;
            _currentFilePath = filePath;

            try
            {
                var oldMedia = _mediaPlayer.Media;
                var media = new LibVLCSharp.Shared.Media(_libVLC!, filePath, FromType.FromPath);
                _mediaPlayer.Media = media;
                
                if (oldMedia != null)
                {
                    oldMedia.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VlcMediaPlayer Load Error] {ex.Message}");
            }
        }

        public void Play()
        {
            try
            {
                _isPlaying = true;
                _mediaPlayer?.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VlcMediaPlayer Play Error] {ex.Message}");
            }
        }

        public void Pause()
        {
            try
            {
                _isPlaying = false;
                _mediaPlayer?.Pause();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VlcMediaPlayer Pause Error] {ex.Message}");
            }
        }

        public void Stop()
        {
            try
            {
                _isPlaying = false;
                _mediaPlayer?.Stop();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VlcMediaPlayer Stop Error] {ex.Message}");
            }
        }

        public void Seek(TimeSpan position)
        {
            _lastPosition = position;
            if (_mediaPlayer != null)
            {
                try
                {
                    _mediaPlayer.Time = (long)position.TotalMilliseconds;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[VlcMediaPlayer Seek Error] {ex.Message}");
                }
            }
        }

        public void SetVolume(double volume)
        {
            _currentVolume = volume;
            if (_mediaPlayer != null)
            {
                try
                {
                    // LibVLC volume is 0 to 100 (or higher)
                    _mediaPlayer.Volume = (int)Math.Clamp(volume, 0, 200);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[VlcMediaPlayer Volume Error] {ex.Message}");
                }
            }
        }

        public void LoadSubtitle(string filePath)
        {
            if (_mediaPlayer != null && File.Exists(filePath))
            {
                try
                {
                    _mediaPlayer.AddSlave(MediaSlaveType.Subtitle, filePath, select: true);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[VlcMediaPlayer Subtitle Error] {ex.Message}");
                }
            }
        }

        public void SetSubtitleVisibility(bool isVisible)
        {
            if (_mediaPlayer != null)
            {
                if (!isVisible)
                {
                    _mediaPlayer.SetSpu(-1);
                }
                else
                {
                    if (_mediaPlayer.Spu == -1)
                    {
                        _mediaPlayer.SetSpu(1);
                    }
                }
            }
        }

        public void SetSubtitleDelay(double delayInSeconds)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.SetSpuDelay((long)(delayInSeconds * 1000000.0));
            }
        }

        public void CycleSubtitleTrack()
        {
            if (_mediaPlayer != null)
            {
                var current = _mediaPlayer.Spu;
                var trackCount = _mediaPlayer.SpuCount;
                if (trackCount > 1)
                {
                    var next = current + 1;
                    if (next >= trackCount) next = -1;
                    _mediaPlayer.SetSpu(next);
                }
            }
        }

        public void CycleAudioTrack()
        {
            if (_mediaPlayer != null)
            {
                var current = _mediaPlayer.AudioTrack;
                var trackCount = _mediaPlayer.AudioTrackCount;
                if (trackCount > 1)
                {
                    var next = (current + 1) % trackCount;
                    if (next == 0) next = 1;
                    _mediaPlayer.SetAudioTrack(next);
                }
            }
        }

        public void SetAudioDelay(double delayInSeconds)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.SetAudioDelay((long)(delayInSeconds * 1000000.0));
            }
        }

        public void SetPlaybackRate(double rate)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.SetRate((float)rate);
            }
        }

        public void Dispose()
        {
            ReleaseNativeResources();
        }

        public System.Collections.Generic.IReadOnlyList<string> GetSubtitleTracks()
        {
            var list = new System.Collections.Generic.List<string>();
            var descriptions = _mediaPlayer?.SpuDescription;
            if (descriptions != null)
            {
                foreach (var desc in descriptions)
                {
                    if (desc.Id != -1)
                    {
                        list.Add(desc.Name ?? $"Track {desc.Id}");
                    }
                }
            }
            return list;
        }

        public int GetActiveSubtitleTrackIndex()
        {
            var descriptions = _mediaPlayer?.SpuDescription;
            if (_mediaPlayer != null && descriptions != null)
            {
                var activeId = _mediaPlayer.Spu;
                int idx = 0;
                foreach (var desc in descriptions)
                {
                    if (desc.Id != -1)
                    {
                        if (desc.Id == activeId) return idx;
                        idx++;
                    }
                }
            }
            return -1;
        }

        public void SetSubtitleTrack(int index)
        {
            var descriptions = _mediaPlayer?.SpuDescription;
            if (_mediaPlayer != null && descriptions != null)
            {
                int idx = 0;
                foreach (var desc in descriptions)
                {
                    if (desc.Id != -1)
                    {
                        if (idx == index)
                        {
                            _mediaPlayer.SetSpu(desc.Id);
                            return;
                        }
                        idx++;
                    }
                }
            }
        }

        public System.Collections.Generic.IReadOnlyList<string> GetAudioTracks()
        {
            var list = new System.Collections.Generic.List<string>();
            var descriptions = _mediaPlayer?.AudioTrackDescription;
            if (descriptions != null)
            {
                foreach (var desc in descriptions)
                {
                    if (desc.Id != -1)
                    {
                        list.Add(desc.Name ?? $"Track {desc.Id}");
                    }
                }
            }
            return list;
        }

        public int GetActiveAudioTrackIndex()
        {
            var descriptions = _mediaPlayer?.AudioTrackDescription;
            if (_mediaPlayer != null && descriptions != null)
            {
                var activeId = _mediaPlayer.AudioTrack;
                int idx = 0;
                foreach (var desc in descriptions)
                {
                    if (desc.Id != -1)
                    {
                        if (desc.Id == activeId) return idx;
                        idx++;
                    }
                }
            }
            return -1;
        }

        public void SetAudioTrack(int index)
        {
            var descriptions = _mediaPlayer?.AudioTrackDescription;
            if (_mediaPlayer != null && descriptions != null)
            {
                int idx = 0;
                foreach (var desc in descriptions)
                {
                    if (desc.Id != -1)
                    {
                        if (idx == index)
                        {
                            _mediaPlayer.SetAudioTrack(desc.Id);
                            return;
                        }
                        idx++;
                    }
                }
            }
        }
    }
}
