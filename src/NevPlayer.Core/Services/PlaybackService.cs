using System;
using System.Collections.Generic;
using NevPlayer.Core.Models;

namespace NevPlayer.Core.Services
{
    public class PlaybackService : IPlaybackService
    {
        private readonly IMediaPlayer _engine;
        private readonly IPlaybackHistoryService? _historyService;
        private List<MediaItem> _queue = new List<MediaItem>();
        private int _currentIndex = -1;
        private bool _isVideoSurfaceReady = false;

        public PlaybackService(IMediaPlayer engine, IPlaybackHistoryService? historyService = null)
        {
            _engine = engine;
            _historyService = historyService;
            
            _engine.StateChanged += (s, state) => State = state;
            _engine.PositionChanged += async (s, pos) => 
            {
                Position = pos;
                // Throttle history updates to every 5 seconds to avoid spamming the disk
                if (pos.TotalSeconds % 5 < 1 && CurrentMedia != null)
                {
                    if (_historyService != null)
                    {
                        await _historyService.UpdateHistoryAsync(CurrentMedia, pos, Duration);
                    }
                }
            };
            
            _engine.DurationLoaded += (s, dur) => 
            {
                if (CurrentMedia != null && _historyService != null)
                {
                    CurrentMedia.Duration = dur;
                    var resumePos = _historyService.GetResumePosition(CurrentMedia.FilePath);
                    if (resumePos.TotalSeconds > 0)
                    {
                        Seek(resumePos);
                    }
                }
            };
        }

        public IMediaPlayer Engine => _engine;

        public MediaItem? CurrentMedia => _currentIndex >= 0 && _currentIndex < _queue.Count ? _queue[_currentIndex] : null;

        private PlaybackState _state = PlaybackState.Idle;
        public PlaybackState State
        {
            get => _state;
            private set
            {
                if (_state != value)
                {
                    _state = value;
                    StateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private double _volume = 100;
        public double Volume
        {
            get => _volume;
            set
            {
                if (_volume != value)
                {
                    _volume = value;
                    _engine.SetVolume(_volume);
                }
            }
        }
        private double _playbackSpeed = 1.0;
        public double PlaybackSpeed
        {
            get => _playbackSpeed;
            set
            {
                if (_playbackSpeed != value)
                {
                    _playbackSpeed = value;
                    _engine.SetPlaybackRate(_playbackSpeed);
                }
            }
        }

        private TimeSpan _position = TimeSpan.Zero;
        public TimeSpan Position
        {
            get => _position;
            private set
            {
                _position = value;
                PositionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public TimeSpan Duration => CurrentMedia?.Duration ?? TimeSpan.Zero;

        public IReadOnlyList<MediaItem> Queue => _queue.AsReadOnly();

        public event EventHandler? StateChanged;
        public event EventHandler? PositionChanged;
        public event EventHandler? MediaChanged;

        public event EventHandler? QueueChanged;
        public event EventHandler<object?>? VideoSurfaceAttached;

        public bool IsVideoSurfaceReady => _isVideoSurfaceReady;

        public void AttachVideoSurface(string[] swapChainOptions)
        {
            _engine.InitializeWithSwapChain(swapChainOptions);
            _isVideoSurfaceReady = true;
            
            VideoSurfaceAttached?.Invoke(this, _engine.NativePlayer);

            if (CurrentMedia != null)
            {
                LoadCurrent();
                Play();
            }
        }

        public void DetachVideoSurface()
        {
            Stop();
            _engine.ReleaseNativeResources();
            _isVideoSurfaceReady = false;
        }

        public void Play()
        {
            if (CurrentMedia != null && _engine.IsInitialized)
            {
                _engine.Play();
            }
        }

        public void Pause()
        {
            if (State == PlaybackState.Playing)
            {
                _engine.Pause();
            }
        }

        public void Stop()
        {
            _engine.Stop();
        }

        public void Next()
        {
            if (_currentIndex < _queue.Count - 1)
            {
                _currentIndex++;
                NotifyMediaChanged();
                _engine.Load(CurrentMedia!.FilePath);
                _engine.Play();
            }
            else
            {
                Stop();
            }
        }

        public void Previous()
        {
            if (_currentIndex > 0)
            {
                _currentIndex--;
                NotifyMediaChanged();
                _engine.Load(CurrentMedia!.FilePath);
                _engine.Play();
            }
            else if (_currentIndex == 0)
            {
                Seek(TimeSpan.Zero);
            }
        }

        public void Seek(TimeSpan position)
        {
            if (position < TimeSpan.Zero) position = TimeSpan.Zero;
            if (position > Duration) position = Duration;
            
            _engine.Seek(position);
        }

        public void Enqueue(MediaItem item, bool autoPlay = true)
        {
            if (item != null)
            {
                _queue.Add(item);
                QueueChanged?.Invoke(this, EventArgs.Empty);
                
                if (_currentIndex == -1)
                {
                    _currentIndex = 0;
                    NotifyMediaChanged();
                    if (autoPlay)
                    {
                        _engine.Load(item.FilePath);
                        _engine.Play();
                    }
                }
            }
        }

        public void LoadCurrent()
        {
            if (CurrentMedia != null)
            {
                _engine.Load(CurrentMedia.FilePath);
            }
        }

        public void RemoveFromQueue(int index)
        {
            if (index >= 0 && index < _queue.Count)
            {
                _queue.RemoveAt(index);
                if (index < _currentIndex)
                {
                    _currentIndex--; // Shift current index back if item before it was removed
                }
                else if (index == _currentIndex)
                {
                    // If we removed the currently playing item
                    Stop();
                    if (_queue.Count > 0)
                    {
                        if (_currentIndex >= _queue.Count) _currentIndex = _queue.Count - 1;
                        NotifyMediaChanged();
                        _engine.Load(CurrentMedia!.FilePath);
                        _engine.Play();
                    }
                    else
                    {
                        _currentIndex = -1;
                        NotifyMediaChanged();
                    }
                }
                QueueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void MoveInQueue(int oldIndex, int newIndex)
        {
            if (oldIndex >= 0 && oldIndex < _queue.Count && newIndex >= 0 && newIndex < _queue.Count && oldIndex != newIndex)
            {
                var item = _queue[oldIndex];
                _queue.RemoveAt(oldIndex);
                _queue.Insert(newIndex, item);
                
                // Fix current index if it was affected
                if (_currentIndex == oldIndex)
                {
                    _currentIndex = newIndex;
                }
                else if (oldIndex < _currentIndex && newIndex >= _currentIndex)
                {
                    _currentIndex--;
                }
                else if (oldIndex > _currentIndex && newIndex <= _currentIndex)
                {
                    _currentIndex++;
                }

                QueueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void PlayQueueItem(int index)
        {
            if (index >= 0 && index < _queue.Count)
            {
                _currentIndex = index;
                NotifyMediaChanged();
                _engine.Load(CurrentMedia!.FilePath);
                _engine.Play();
            }
        }

        public void ClearQueue()
        {
            _queue.Clear();
            _currentIndex = -1;
            QueueChanged?.Invoke(this, EventArgs.Empty);
            Stop();
            NotifyMediaChanged();
        }

        private void NotifyMediaChanged()
        {
            MediaChanged?.Invoke(this, EventArgs.Empty);
            Position = TimeSpan.Zero;
        }

        public void LoadSubtitle(string filePath)
        {
            _engine.LoadSubtitle(filePath);
        }

        public void SetSubtitleVisibility(bool isVisible)
        {
            _engine.SetSubtitleVisibility(isVisible);
        }

        public void SetSubtitleDelay(double delayInSeconds)
        {
            _engine.SetSubtitleDelay(delayInSeconds);
        }

        public void CycleSubtitleTrack()
        {
            _engine.CycleSubtitleTrack();
        }

        public void CycleAudioTrack()
        {
            _engine.CycleAudioTrack();
        }

        public void SetAudioDelay(double delayInSeconds)
        {
            _engine.SetAudioDelay(delayInSeconds);
        }
    }
}
