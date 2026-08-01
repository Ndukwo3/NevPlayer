using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NevPlayer.App.Services;
using NevPlayer.Core.Services;

namespace NevPlayer.App.Views
{
    public sealed partial class CinemaPage : Page
    {
        private readonly IPlaybackService _playbackService;
        private DispatcherTimer _osdTimer;

        // Held so we can subscribe/unsubscribe from native MediaPlayer events cleanly.
        private Windows.Media.Playback.MediaPlayer? _nativePlayer;

        public CinemaPage()
        {
            this.InitializeComponent();

            _playbackService = App.PlaybackService!;

            _osdTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _osdTimer.Tick += OsdTimer_Tick;

            Loaded += CinemaPage_Loaded;
            Unloaded += CinemaPage_Unloaded;

            // Subscribe to playback events
            _playbackService.PositionChanged += PlaybackService_PositionChanged;
            _playbackService.StateChanged    += PlaybackService_StateChanged;
            _playbackService.Engine.DurationLoaded += Engine_DurationLoaded;
            _playbackService.MediaChanged    += PlaybackService_MediaChanged;

            // Subscribe to engine-level failure / end events
            if (_playbackService.Engine is WindowsMediaPlayer wmp)
            {
                wmp.PlaybackFailed += Engine_PlaybackFailed;
                wmp.MediaEnded     += Engine_MediaEnded;
            }

            // Hook up context menus and sub-menus
            PlayerContextMenu.Opening += PlayerContextMenu_Opening;
            SubtitleMenuFlyout.Opening += SubtitleMenuFlyout_Opening;
            AudioMenuFlyout.Opening += AudioMenuFlyout_Opening;

            // Handle spacebar globally before child controls capture it
            PreviewKeyDown += CinemaPage_PreviewKeyDown;
        }

        private void OsdTimer_Tick(object? sender, object e)
        {
            _osdTimer.Stop();
            OsdFadeOutStoryboard.Begin();
        }

        private void CinemaPage_PreviewKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Space)
            {
                e.Handled = true; // Stop focus bubble so it doesn't click other focused buttons
                
                // Toggle play/pause
                if (_playbackService.State == NevPlayer.Core.Models.PlaybackState.Playing)
                {
                    _playbackService.Pause();
                    ShowOsd("Pause");
                }
                else
                {
                    _playbackService.Play();
                    ShowOsd("Play");
                }
            }
        }

        private void ShowOsd(string text)
        {
            if (OsdText == null || _osdTimer == null) return;
            OsdText.Text = text;
            OsdFadeInStoryboard.Begin();
            _osdTimer.Stop();
            _osdTimer.Start();
        }

        private void CinemaPage_Loaded(object? sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[NevPlayer Diagnostics] CinemaPage_Loaded invoked.");
            System.Diagnostics.Debug.WriteLine($"[NevPlayer Diagnostics] VideoSurface Visibility: {VideoSurface?.Visibility}, ActualWidth: {VideoSurface?.ActualWidth}, ActualHeight: {VideoSurface?.ActualHeight}");

            if (_playbackService.Engine.NativePlayer is Windows.Media.Playback.MediaPlayer nativePlayer)
            {
                System.Diagnostics.Debug.WriteLine("[NevPlayer Diagnostics] NativePlayer found. Connecting to VideoSurface...");
                _nativePlayer = nativePlayer;

                // Initial surface connection (handles the case where media is already playing
                // when we navigate to CinemaPage, e.g. synchronously-loaded MKV).
                VideoSurface?.SetMediaPlayer(nativePlayer);
                System.Diagnostics.Debug.WriteLine("[NevPlayer Diagnostics] VideoSurface.SetMediaPlayer(_nativePlayer) executed successfully.");

                // WINUI 3 BUG FIX:
                // If the player is already playing when the surface is first attached, the video 
                // pipeline might not render frames (black screen). A quick seek or pause/play forces a render.
                if (_nativePlayer.PlaybackSession.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Playing)
                {
                    System.Diagnostics.Debug.WriteLine("[NevPlayer Diagnostics] NativePlayer is already playing. Initiating Pause/Play kick to force video frame rendering.");
                    _nativePlayer.Pause();
                    _nativePlayer.Play();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[NevPlayer Diagnostics] WARNING: NativePlayer is null or not a Windows.Media.Playback.MediaPlayer!");
            }
            
            if (VolumeSlider != null)
            {
                VolumeSlider.Value = _playbackService.Volume;
            }
            
            PlaylistView.ItemsSource = _playlistItems;
            _playbackService.QueueChanged += PlaybackService_QueueChanged;
            
            UpdateMetadata();
        }

        private void CinemaPage_Unloaded(object? sender, RoutedEventArgs e)
        {
            // Do NOT set _nativePlayer to null or disconnect the surface here.
            // With NavigationCacheMode="Required", the page stays alive. Disconnecting
            // the surface causes the WinUI 3 black screen bug when navigating back.
            
            _playbackService.PositionChanged -= PlaybackService_PositionChanged;
            _playbackService.StateChanged    -= PlaybackService_StateChanged;
            _playbackService.Engine.DurationLoaded -= Engine_DurationLoaded;
            _playbackService.MediaChanged    -= PlaybackService_MediaChanged;
            _playbackService.QueueChanged    -= PlaybackService_QueueChanged;

            if (_playbackService.Engine is WindowsMediaPlayer wmp)
            {
                wmp.PlaybackFailed -= Engine_PlaybackFailed;
                wmp.MediaEnded     -= Engine_MediaEnded;
            }
        }

        private void PlaybackService_MediaChanged(object? sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                UpdateMetadata();
            });
        }

        private void UpdateMetadata()
        {
            var media = _playbackService.CurrentMedia;
            if (media != null && MediaTitleText != null && MediaMetadataText != null)
            {
                MediaTitleText.Text = string.IsNullOrEmpty(media.Title) ? System.IO.Path.GetFileNameWithoutExtension(media.FilePath) : media.Title;
                
                var metaParts = new System.Collections.Generic.List<string>();
                if (!string.IsNullOrEmpty(media.Resolution)) metaParts.Add(media.Resolution);
                if (media.Year > 0) metaParts.Add(media.Year.ToString());
                if (media.Bitrate > 0) metaParts.Add($"{media.Bitrate / 1000} kbps");
                
                MediaMetadataText.Text = metaParts.Count > 0 ? string.Join(" • ", metaParts) : "";
            }
        }

        private bool _isDraggingSlider = false;

        private void PlaybackService_PositionChanged(object? sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (!_isDraggingSlider && TimelineSlider != null)
                {
                    TimelineSlider.Value = _playbackService.Position.TotalSeconds;
                    CurrentTimeText.Text = _playbackService.Position.ToString(@"hh\:mm\:ss");
                }
            });
        }

        private void Engine_DurationLoaded(object? sender, TimeSpan duration)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (TimelineSlider != null)
                {
                    TimelineSlider.Maximum = duration.TotalSeconds;
                    TotalTimeText.Text = duration.ToString(@"hh\:mm\:ss");
                }
            });
        }

        private void PlaybackService_StateChanged(object? sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (PlayPauseButton != null)
                {
                    PlayPauseButton.Content = _playbackService.State == NevPlayer.Core.Models.PlaybackState.Playing ? "\uE103" : "\uE102"; // Pause : Play icon
                }
            });
        }

        private void Engine_PlaybackFailed(object? sender, string errorMessage)
        {
            DispatcherQueue.TryEnqueue(async () =>
            {
                var dialog = new ContentDialog
                {
                    Title           = "Playback Error",
                    Content         = errorMessage + "\n\nTip: Install the free codec packs from the Microsoft Store (\"HEVC Video Extensions\" or \"AV1 Video Extension\") for unsupported formats.",
                    CloseButtonText = "OK",
                    XamlRoot        = this.XamlRoot
                };
                await dialog.ShowAsync();
            });
        }

        private void Engine_MediaEnded(object? sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                // Auto-advance to the next item in the queue
                _playbackService.Next();
            });
        }

        private void TimelineSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            // Only seek if the user is interacting with the slider directly, 
            // not when we update it programmatically from PositionChanged
            // In a real app we'd use PointerCapture or thumb drag events, but for now we can seek on value change if it's a large jump.
            if (Math.Abs(e.NewValue - e.OldValue) > 1.5 && _playbackService != null)
            {
                _playbackService.Seek(TimeSpan.FromSeconds(e.NewValue));
            }
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (_playbackService.State == NevPlayer.Core.Models.PlaybackState.Playing)
            {
                _playbackService.Pause();
            }
            else
            {
                _playbackService.Play();
            }
        }

        private void SkipBackward_Click(object sender, RoutedEventArgs e)
        {
            var newPos = _playbackService.Position - TimeSpan.FromSeconds(10);
            if (newPos < TimeSpan.Zero) newPos = TimeSpan.Zero;
            _playbackService.Seek(newPos);
        }

        private void SkipForward_Click(object sender, RoutedEventArgs e)
        {
            var newPos = _playbackService.Position + TimeSpan.FromSeconds(10);
            if (newPos > _playbackService.Duration) newPos = _playbackService.Duration;
            _playbackService.Seek(newPos);
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            _playbackService.Previous();
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            _playbackService.Next();
        }

        // --- Subtitle & Audio Track Selection UI logic ---

        private async void LoadSubtitle_Click(object? sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            
            // In WinUI 3, we must initialize the picker with the main window handle
            var window = (Microsoft.UI.Xaml.Application.Current as App)?.MainWindow;
            if (window != null)
            {
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hWnd);
            }

            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
            picker.FileTypeFilter.Add(".srt");
            picker.FileTypeFilter.Add(".ass");
            picker.FileTypeFilter.Add(".vtt");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                _playbackService.LoadSubtitle(file.Path);
            }
        }

        private UIElement? GetAppTitleBar()
        {
            var app = Application.Current as App;
            var win = app?.MainWindow;
            if (win?.Content is ShellPage shell)
            {
                return shell.AppTitleBarElement;
            }
            if (win?.Content is Frame frame && frame.Content is ShellPage shellPage)
            {
                return shellPage.AppTitleBarElement;
            }
            return null;
        }

        private void PlayerContextMenu_Opening(object? sender, object e)
        {
            var menu = PlayerContextMenu;
            menu.Items.Clear();

            // Open Subtitle option
            var openSubItem = new MenuFlyoutItem { Text = "Open Subtitle...", Icon = new SymbolIcon(Symbol.Document) };
            openSubItem.Click += LoadSubtitle_Click;
            menu.Items.Add(openSubItem);

            menu.Items.Add(new MenuFlyoutSeparator());

            // Playback Options sub-menu
            var playbackSubMenu = new MenuFlyoutSubItem { Text = "Playback" };
            var playItem = new MenuFlyoutItem { Text = _playbackService.State == NevPlayer.Core.Models.PlaybackState.Playing ? "Pause" : "Play" };
            playItem.Click += PlayPause_Click;
            playbackSubMenu.Items.Add(playItem);
            
            var stopItem = new MenuFlyoutItem { Text = "Stop" };
            stopItem.Click += (s, a) => _playbackService.Stop();
            playbackSubMenu.Items.Add(stopItem);
            menu.Items.Add(playbackSubMenu);

            // Subtitles sub-menu (PotPlayer layout)
            var subtitleSubMenu = new MenuFlyoutSubItem { Text = "Subtitles" };
            PopulateSubtitleMenuHelper(subtitleSubMenu.Items);
            menu.Items.Add(subtitleSubMenu);

            // Audio sub-menu (PotPlayer layout)
            var audioSubMenu = new MenuFlyoutSubItem { Text = "Audio" };
            PopulateAudioMenuHelper(audioSubMenu.Items);
            menu.Items.Add(audioSubMenu);

            // Aspect Ratio sub-menu
            var arSubMenu = new MenuFlyoutSubItem { Text = "Aspect Ratio" };
            var arFit = new MenuFlyoutItem { Text = "Fit (Uniform)" };
            arFit.Click += (s, a) => { if (VideoSurface != null) { VideoSurface.Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform; ShowOsd("Aspect: Fit"); } };
            var arFill = new MenuFlyoutItem { Text = "Fill (UniformToFill)" };
            arFill.Click += (s, a) => { if (VideoSurface != null) { VideoSurface.Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill; ShowOsd("Aspect: Fill"); } };
            var arStretch = new MenuFlyoutItem { Text = "Stretch" };
            arStretch.Click += (s, a) => { if (VideoSurface != null) { VideoSurface.Stretch = Microsoft.UI.Xaml.Media.Stretch.Fill; ShowOsd("Aspect: Stretch"); } };
            arSubMenu.Items.Add(arFit);
            arSubMenu.Items.Add(arFill);
            arSubMenu.Items.Add(arStretch);
            menu.Items.Add(arSubMenu);

            menu.Items.Add(new MenuFlyoutSeparator());

            // Fullscreen option
            var titleBar = GetAppTitleBar();
            var fsItem = new ToggleMenuFlyoutItem { Text = "Fullscreen", IsChecked = titleBar == null || titleBar.Visibility == Visibility.Collapsed };
            fsItem.Click += (s, a) =>
            {
                var app = Application.Current as App;
                var win = app?.MainWindow;
                if (win != null)
                {
                    var presenter = win.AppWindow.Presenter;
                    var bar = GetAppTitleBar();
                    if (presenter.Kind == Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen)
                    {
                        win.AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.Default);
                        if (bar != null) bar.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        win.AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);
                        if (bar != null) bar.Visibility = Visibility.Collapsed;
                    }
                }
            };
            menu.Items.Add(fsItem);
        }

        private void SubtitleMenuFlyout_Opening(object? sender, object e)
        {
            SubtitleMenuFlyout.Items.Clear();
            PopulateSubtitleMenuHelper(SubtitleMenuFlyout.Items);
        }

        private void AudioMenuFlyout_Opening(object? sender, object e)
        {
            AudioMenuFlyout.Items.Clear();
            PopulateAudioMenuHelper(AudioMenuFlyout.Items);
        }

        private void PopulateSubtitleMenuHelper(System.Collections.Generic.IList<MenuFlyoutItemBase> itemsList)
        {
            // Show/Hide Subtitles Toggle
            var isSubVisible = true; // Default to true
            var showHideToggle = new ToggleMenuFlyoutItem { Text = "Show/Hide Subtitles", IsChecked = isSubVisible };
            showHideToggle.Click += (s, a) =>
            {
                isSubVisible = !isSubVisible;
                _playbackService.SetSubtitleVisibility(isSubVisible);
                ShowOsd(isSubVisible ? "Subtitles: Enabled" : "Subtitles: Disabled");
            };
            itemsList.Add(showHideToggle);

            // Load External Subtitle
            var loadExtItem = new MenuFlyoutItem { Text = "Load External Subtitle..." };
            loadExtItem.Click += LoadSubtitle_Click;
            itemsList.Add(loadExtItem);

            var configSubItem = new MenuFlyoutItem { Text = "Configure Subtitles..." };
            configSubItem.Click += (s, a) => ShowSubtitleConfigDialog();
            itemsList.Add(configSubItem);

            itemsList.Add(new MenuFlyoutSeparator());

            // List available embedded tracks
            var playbackItem = _nativePlayer?.Source as Windows.Media.Playback.MediaPlaybackItem;
            if (playbackItem != null)
            {
                var tracks = playbackItem.TimedMetadataTracks;
                if (tracks.Count > 0)
                {
                    for (int i = 0; i < tracks.Count; i++)
                    {
                        var track = tracks[i];
                        if (track.TimedMetadataKind == Windows.Media.Core.TimedMetadataKind.Subtitle || 
                            track.TimedMetadataKind == Windows.Media.Core.TimedMetadataKind.ImageSubtitle)
                        {
                            var trackIndex = i;
                            var lang = string.IsNullOrWhiteSpace(track.Language) ? "Unknown" : track.Language;
                            var label = string.IsNullOrWhiteSpace(track.Label) ? $"Track {trackIndex + 1}" : track.Label;
                            var mode = tracks.GetPresentationMode((uint)trackIndex);
                            
                            var trackItem = new ToggleMenuFlyoutItem 
                            { 
                                Text = $"*Text - {lang} ({label})",
                                IsChecked = mode == Windows.Media.Playback.TimedMetadataTrackPresentationMode.PlatformPresented
                            };
                            trackItem.Click += (s, a) =>
                            {
                                // Disable previous selected tracks first
                                for (int j = 0; j < tracks.Count; j++)
                                {
                                    tracks.SetPresentationMode((uint)j, Windows.Media.Playback.TimedMetadataTrackPresentationMode.Disabled);
                                }
                                // Enable this track
                                tracks.SetPresentationMode((uint)trackIndex, Windows.Media.Playback.TimedMetadataTrackPresentationMode.PlatformPresented);
                                ShowOsd($"Subtitles: {lang}");
                            };
                            itemsList.Add(trackItem);
                        }
                    }
                }
                else
                {
                    var emptyText = new MenuFlyoutItem { Text = "No Subtitles Detected", IsEnabled = false };
                    itemsList.Add(emptyText);
                }
            }
            else
            {
                var emptyText = new MenuFlyoutItem { Text = "No Media Loaded", IsEnabled = false };
                itemsList.Add(emptyText);
            }
        }

        private void PopulateAudioMenuHelper(System.Collections.Generic.IList<MenuFlyoutItemBase> itemsList)
        {
            // Next Track
            var nextAudio = new MenuFlyoutItem { Text = "Cycle Audio Track" };
            nextAudio.Click += CycleAudio_Click;
            itemsList.Add(nextAudio);

            itemsList.Add(new MenuFlyoutSeparator());

            // List available audio tracks
            var playbackItem = _nativePlayer?.Source as Windows.Media.Playback.MediaPlaybackItem;
            if (playbackItem != null)
            {
                var tracks = playbackItem.AudioTracks;
                if (tracks.Count > 0)
                {
                    int activeIndex = tracks.SelectedIndex;
                    for (int i = 0; i < tracks.Count; i++)
                    {
                        var track = tracks[i];
                        var trackIndex = i;
                        var lang = string.IsNullOrWhiteSpace(track.Language) ? "Unknown" : track.Language;
                        var label = string.IsNullOrWhiteSpace(track.Label) ? $"Audio {trackIndex + 1}" : track.Label;
                        
                        var trackItem = new ToggleMenuFlyoutItem 
                        { 
                            Text = $"{lang} ({label})",
                            IsChecked = trackIndex == activeIndex
                        };
                        trackItem.Click += (s, a) =>
                        {
                            tracks.SelectedIndex = trackIndex;
                            ShowOsd($"Audio: {lang}");
                        };
                        itemsList.Add(trackItem);
                    }
                }
                else
                {
                    var emptyText = new MenuFlyoutItem { Text = "No Audio Streams Detected", IsEnabled = false };
                    itemsList.Add(emptyText);
                }
            }
            else
            {
                var emptyText = new MenuFlyoutItem { Text = "No Media Loaded", IsEnabled = false };
                itemsList.Add(emptyText);
            }
        }

        private void CycleSubtitle_Click(object sender, RoutedEventArgs e)
        {
            _playbackService?.CycleSubtitleTrack();
        }

        private void CycleAudio_Click(object sender, RoutedEventArgs e)
        {
            _playbackService?.CycleAudioTrack();
        }

        private void VolumeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (_playbackService != null)
            {
                _playbackService.Volume = e.NewValue;
                // Avoid showing OSD on initial page load if VolumeSlider initializes its value
                if (IsLoaded)
                {
                    ShowOsd($"Volume: {e.NewValue:0}%");
                }
            }
        }

        private void Grid_PointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var pointerPoint = e.GetCurrentPoint(this);
            var delta = pointerPoint.Properties.MouseWheelDelta;

            var volumeChange = (delta / 120.0) * 5.0;

            if (VolumeSlider != null)
            {
                var newVolume = VolumeSlider.Value + volumeChange;
                newVolume = Math.Max(0, Math.Min(100, newVolume));
                VolumeSlider.Value = newVolume;
            }
            
            e.Handled = true;
        }

        // --- Playlist System ---
        private readonly IPlaylistService _playlistService = new PlaylistService();
        private readonly System.Collections.ObjectModel.ObservableCollection<NevPlayer.Core.Models.MediaItem> _playlistItems = new System.Collections.ObjectModel.ObservableCollection<NevPlayer.Core.Models.MediaItem>();

        private void PlaybackService_QueueChanged(object? sender, EventArgs e)
        {
            _playlistItems.Clear();
            foreach (var item in _playbackService.Queue)
            {
                _playlistItems.Add(item);
            }
        }

        private async void AddPlaylist_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            var window = (Microsoft.UI.Xaml.Application.Current as App)?.MainWindow;
            if (window != null)
            {
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hWnd);
            }
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
            // All supported video formats
            foreach (var ext in new[]
            {
                ".mp4", ".mkv", ".avi", ".webm", ".mov", ".wmv",
                ".flv", ".m4v", ".ts", ".m2ts", ".vob", ".3gp",
                ".mpeg", ".mpg", ".divx", ".xvid", ".rmvb", ".asf",
                // Audio formats
                ".mp3", ".flac", ".aac", ".wav", ".ogg", ".wma", ".m4a", ".opus"
            })
            {
                picker.FileTypeFilter.Add(ext);
            }
            
            var files = await picker.PickMultipleFilesAsync();
            foreach (var file in files)
            {
                _playbackService.Enqueue(new NevPlayer.Core.Models.MediaItem 
                { 
                    Title = file.Name,
                    FilePath = file.Path
                });
            }
        }

        private void ClearPlaylist_Click(object sender, RoutedEventArgs e)
        {
            _playbackService.ClearQueue();
        }

        private async void SavePlaylist_Click(object sender, RoutedEventArgs e)
        {
            // For prototype phase, save a default named playlist
            await _playlistService.SavePlaylistAsync("MyPlaylist", _playbackService.Queue);
            ShowOsd("Playlist Saved");
        }

        private void PlaylistView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is NevPlayer.Core.Models.MediaItem mediaItem)
            {
                var index = _playlistItems.IndexOf(mediaItem);
                if (index >= 0)
                {
                    _playbackService.PlayQueueItem(index);
                }
            }
        }

        private void SpeedComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SpeedComboBox?.SelectedItem is ComboBoxItem item && double.TryParse(item.Tag?.ToString(), out double speed))
            {
                if (_playbackService != null)
                {
                    _playbackService.PlaybackSpeed = speed;
                    ShowOsd($"Speed: {speed}x");
                }
            }
        }

        private void AspectRatioComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AspectRatioComboBox?.SelectedItem is ComboBoxItem item && VideoSurface != null)
            {
                string? tag = item.Tag?.ToString();
                if (tag == "Uniform")
                {
                    VideoSurface.Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform;
                    ShowOsd("Aspect: Fit");
                }
                else if (tag == "UniformToFill")
                {
                    VideoSurface.Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill;
                    ShowOsd("Aspect: Fill");
                }
                else if (tag == "Stretch")
                {
                    VideoSurface.Stretch = Microsoft.UI.Xaml.Media.Stretch.Fill;
                    ShowOsd("Aspect: Stretch");
                }
            }
        }

        private double _dialogSubtitleDelay = 0.0;
        private async void ShowSubtitleConfigDialog()
        {
            var stackPanel = new StackPanel { Spacing = 16, Width = 320 };

            // Subtitle Delay Slider
            var delayHeader = new TextBlock { Text = $"Subtitle Delay: {_dialogSubtitleDelay:F1}s", Style = (Style)Application.Current.Resources["NevBodyTextBlockStyle"], FontWeight = Microsoft.UI.Text.FontWeights.Bold };
            var delaySlider = new Slider 
            { 
                Minimum = -10, 
                Maximum = 10, 
                StepFrequency = 0.5, 
                Value = _dialogSubtitleDelay, 
                HorizontalAlignment = HorizontalAlignment.Stretch 
            };
            delaySlider.ValueChanged += (s, e) =>
            {
                _dialogSubtitleDelay = e.NewValue;
                delayHeader.Text = $"Subtitle Delay: {_dialogSubtitleDelay:F1}s";
                _playbackService.SetSubtitleDelay(_dialogSubtitleDelay);
            };

            // Font Size Slider (Custom UI config simulation)
            var sizeHeader = new TextBlock { Text = "Subtitle Font Size: 24px", Style = (Style)Application.Current.Resources["NevBodyTextBlockStyle"], FontWeight = Microsoft.UI.Text.FontWeights.Bold };
            var sizeSlider = new Slider 
            { 
                Minimum = 12, 
                Maximum = 48, 
                StepFrequency = 1, 
                Value = 24, 
                HorizontalAlignment = HorizontalAlignment.Stretch 
            };
            sizeSlider.ValueChanged += (s, e) =>
            {
                sizeHeader.Text = $"Subtitle Font Size: {e.NewValue:0}px";
                // System-presented subtitles font size changes can be styled in Windows accessibility settings, 
                // but this represents the custom subtitle styling controls.
                System.Diagnostics.Debug.WriteLine($"[NevPlayer Diagnostics] Subtitle Font Size set to: {e.NewValue}px");
            };

            stackPanel.Children.Add(delayHeader);
            stackPanel.Children.Add(delaySlider);
            stackPanel.Children.Add(sizeHeader);
            stackPanel.Children.Add(sizeSlider);

            var dialog = new ContentDialog
            {
                Title = "Subtitle Settings",
                Content = stackPanel,
                CloseButtonText = "Done",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}
