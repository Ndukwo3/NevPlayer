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
        private Windows.Media.Core.TimedMetadataTrack? _activeSubtitleTrack;

        public CinemaPage()
        {
            this.InitializeComponent();

            _playbackService = App.PlaybackService!;

            _osdTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _osdTimer.Tick += OsdTimer_Tick;

            Loaded += CinemaPage_Loaded;
            Unloaded += CinemaPage_Unloaded;

            // Hook up context menus and sub-menus
            PlayerContextMenu.Opening += PlayerContextMenu_Opening;
            SubtitleMenuFlyout.Opening += SubtitleMenuFlyout_Opening;
            AudioMenuFlyout.Opening += AudioMenuFlyout_Opening;
        }

        /// <summary>
        /// Connect the media player to the video surface as EARLY as possible —
        /// before the visual tree even finishes rendering. This is the primary fix
        /// for the black video screen bug in WinUI 3 MediaPlayerElement.
        /// </summary>
        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (_playbackService.Engine.NativePlayer is Windows.Media.Playback.MediaPlayer nativePlayer)
            {
                _nativePlayer = nativePlayer;
                // Clear any old surface first, then bind fresh
                VideoSurface?.SetMediaPlayer(null);
                VideoSurface?.SetMediaPlayer(nativePlayer);
                System.Diagnostics.Debug.WriteLine(\"[NevPlayer Diagnostics] OnNavigatedTo: VideoSurface connected.\");
            }
        }

        private void OsdTimer_Tick(object? sender, object e)
        {
            _osdTimer.Stop();
            OsdFadeOutStoryboard.Begin();
        }

        private void Spacebar_Invoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true; // Prevents the keypress from triggering focused controls
            
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

            // Subscribe to playback events on load
            _playbackService.PositionChanged += PlaybackService_PositionChanged;
            _playbackService.StateChanged    += PlaybackService_StateChanged;
            _playbackService.Engine.DurationLoaded += Engine_DurationLoaded;
            _playbackService.MediaChanged    += PlaybackService_MediaChanged;
            _playbackService.QueueChanged    += PlaybackService_QueueChanged;

            if (_playbackService.Engine is WindowsMediaPlayer wmp)
            {
                wmp.PlaybackFailed += Engine_PlaybackFailed;
                wmp.MediaEnded     += Engine_MediaEnded;
            }

            if (_playbackService.Engine.NativePlayer is Windows.Media.Playback.MediaPlayer nativePlayer)
            {
                System.Diagnostics.Debug.WriteLine("[NevPlayer Diagnostics] NativePlayer found. Connecting to VideoSurface...");
                _nativePlayer = nativePlayer;

                // Step 1: clear any stale surface binding first
                VideoSurface?.SetMediaPlayer(null);

                // Step 2: attach the player to the MediaPlayerElement
                VideoSurface?.SetMediaPlayer(nativePlayer);
                System.Diagnostics.Debug.WriteLine("[NevPlayer Diagnostics] VideoSurface.SetMediaPlayer() executed.");

                // Step 3: After the page layout fully renders (one frame later), do a
                // detach + re-attach so the compositor picks up the live video surface.
                // This is the reliable fix for black video in WinUI 3 when the player
                // is already playing before the page is navigated to.
                AttachVideoSurfaceWithDelay(nativePlayer);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[NevPlayer Diagnostics] WARNING: NativePlayer is null!");
            }
            
            if (VolumeSlider != null)
            {
                VolumeSlider.Value = _playbackService.Volume;
            }
            
            PlaylistView.ItemsSource = _playlistItems;
            
            // Re-sync progress bar maximum and value immediately upon loading
            if (TimelineSlider != null)
            {
                TimelineSlider.Maximum = _playbackService.Duration.TotalSeconds;
                TimelineSlider.Value = _playbackService.Position.TotalSeconds;
                CurrentTimeText.Text = _playbackService.Position.ToString(@"hh\:mm\:ss");
                TotalTimeText.Text = _playbackService.Duration.ToString(@"hh\:mm\:ss");
            }

            // Sync play/pause button state
            if (PlayPauseButton != null)
            {
                PlayPauseButton.Content = _playbackService.State == NevPlayer.Core.Models.PlaybackState.Playing ? "\uE103" : "\uE102";
            }

            UpdateMetadata();
        }

        /// <summary>
        /// Fires a one-shot 300ms timer that disconnects and reconnects the video surface.
        /// This forces the WinUI 3 compositor to re-acquire the MediaPlayer render target
        /// after the page's visual tree has had one full layout pass, resolving the
        /// black-screen bug that occurs when the player is already streaming on navigation.
        /// </summary>
        private void AttachVideoSurfaceWithDelay(Windows.Media.Playback.MediaPlayer player)
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                System.Diagnostics.Debug.WriteLine("[NevPlayer Diagnostics] Delayed re-attach: clearing surface...");
                VideoSurface?.SetMediaPlayer(null);
                System.Diagnostics.Debug.WriteLine("[NevPlayer Diagnostics] Delayed re-attach: binding surface...");
                VideoSurface?.SetMediaPlayer(player);
                System.Diagnostics.Debug.WriteLine("[NevPlayer Diagnostics] Delayed re-attach complete.");
            };
            timer.Start();
        }

        private void CinemaPage_Unloaded(object? sender, RoutedEventArgs e)
        {
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

            if (_activeSubtitleTrack != null)
            {
                _activeSubtitleTrack.CueEntered -= ActiveTrack_CueEntered;
                _activeSubtitleTrack.CueExited -= ActiveTrack_CueExited;
                _activeSubtitleTrack = null;
            }

            StopVisualizerAnimation();
        }

        private void PlaybackService_MediaChanged(object? sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                UpdateMetadata();
            });
        }

        private DispatcherTimer? _visualizerTimer;
        private readonly Random _rand = new Random();

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

                // Decide whether to show Video Surface or Audio Music Visualizer
                bool isAudio = !media.IsVideo;
                if (isAudio)
                {
                    AudioVisualizerGrid.Visibility = Visibility.Visible;
                    
                    // Load album art
                    if (!string.IsNullOrEmpty(media.AlbumArtPath))
                    {
                        VisualizerAlbumArt.ImageSource = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(media.AlbumArtPath));
                    }
                    else
                    {
                        VisualizerAlbumArt.ImageSource = null;
                    }

                    StartVisualizerAnimation();
                }
                else
                {
                    AudioVisualizerGrid.Visibility = Visibility.Collapsed;
                    StopVisualizerAnimation();
                }
            }
        }

        private void StartVisualizerAnimation()
        {
            if (_visualizerTimer == null)
            {
                _visualizerTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
                _visualizerTimer.Tick += VisualizerTimer_Tick;
            }

            if (_playbackService.State == NevPlayer.Core.Models.PlaybackState.Playing)
            {
                _visualizerTimer.Start();
            }
            else
            {
                _visualizerTimer.Stop();
                ResetVisualizerBars();
            }
        }

        private void StopVisualizerAnimation()
        {
            _visualizerTimer?.Stop();
            ResetVisualizerBars();
        }

        private double _currentRotationAngle = 0;

        private void ResetVisualizerBars()
        {
            // Set all visualizer bars back to a default low height
            if (VisualizerBar1 != null) VisualizerBar1.Height = 15;
            if (VisualizerBar2 != null) VisualizerBar2.Height = 25;
            if (VisualizerBar3 != null) VisualizerBar3.Height = 15;
            if (VisualizerBar4 != null) VisualizerBar4.Height = 35;
            if (VisualizerBar5 != null) VisualizerBar5.Height = 20;
            if (VisualizerBar6 != null) VisualizerBar6.Height = 30;
            if (VisualizerBar7 != null) VisualizerBar7.Height = 25;
            if (VisualizerBar8 != null) VisualizerBar8.Height = 40;
            if (VisualizerBar9 != null) VisualizerBar9.Height = 20;
            if (VisualizerBar10 != null) VisualizerBar10.Height = 25;
            if (VisualizerBar11 != null) VisualizerBar11.Height = 15;
            if (VisualizerBar12 != null) VisualizerBar12.Height = 30;
            if (VisualizerBar13 != null) VisualizerBar13.Height = 20;
            if (VisualizerBar14 != null) VisualizerBar14.Height = 25;
            if (VisualizerBar15 != null) VisualizerBar15.Height = 15;
        }

        private void VisualizerTimer_Tick(object? sender, object e)
        {
            if (_playbackService.State != NevPlayer.Core.Models.PlaybackState.Playing)
            {
                _visualizerTimer?.Stop();
                ResetVisualizerBars();
                return;
            }

            // Slowly rotate the disc card while playing
            if (DiscRotation != null)
            {
                _currentRotationAngle = (_currentRotationAngle + 2.0) % 360;
                DiscRotation.Angle = _currentRotationAngle;
            }

            // Animate each bar to bounce randomly representing live audio frequencies with larger maximum heights (up to 150px)
            if (VisualizerBar1 != null) VisualizerBar1.Height = _rand.Next(15, 90);
            if (VisualizerBar2 != null) VisualizerBar2.Height = _rand.Next(25, 120);
            if (VisualizerBar3 != null) VisualizerBar3.Height = _rand.Next(15, 75);
            if (VisualizerBar4 != null) VisualizerBar4.Height = _rand.Next(35, 150);
            if (VisualizerBar5 != null) VisualizerBar5.Height = _rand.Next(20, 110);
            if (VisualizerBar6 != null) VisualizerBar6.Height = _rand.Next(30, 140);
            if (VisualizerBar7 != null) VisualizerBar7.Height = _rand.Next(20, 85);
            if (VisualizerBar8 != null) VisualizerBar8.Height = _rand.Next(40, 160);
            if (VisualizerBar9 != null) VisualizerBar9.Height = _rand.Next(20, 115);
            if (VisualizerBar10 != null) VisualizerBar10.Height = _rand.Next(25, 130);
            if (VisualizerBar11 != null) VisualizerBar11.Height = _rand.Next(15, 80);
            if (VisualizerBar12 != null) VisualizerBar12.Height = _rand.Next(30, 140);
            if (VisualizerBar13 != null) VisualizerBar13.Height = _rand.Next(20, 100);
            if (VisualizerBar14 != null) VisualizerBar14.Height = _rand.Next(25, 115);
            if (VisualizerBar15 != null) VisualizerBar15.Height = _rand.Next(15, 85);
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
                    // Fixed: was hh:\ss (missing minutes). Correct format: hh:mm:ss
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
                
                var media = _playbackService.CurrentMedia;
                if (media != null && !media.IsVideo)
                {
                    StartVisualizerAnimation();
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
            var isSubVisible = _activeSubtitleTrack != null;
            var showHideToggle = new ToggleMenuFlyoutItem { Text = "Show/Hide Subtitles", IsChecked = isSubVisible };
            showHideToggle.Click += (s, a) =>
            {
                if (showHideToggle.IsChecked)
                {
                    // Find first subtitle track and activate
                    var playbackItem = _nativePlayer?.Source as Windows.Media.Playback.MediaPlaybackItem;
                    if (playbackItem != null && playbackItem.TimedMetadataTracks.Count > 0)
                    {
                        var tracks = playbackItem.TimedMetadataTracks;
                        for (int i = 0; i < tracks.Count; i++)
                        {
                            var track = tracks[i];
                            if (track.TimedMetadataKind == Windows.Media.Core.TimedMetadataKind.Subtitle || 
                                track.TimedMetadataKind == Windows.Media.Core.TimedMetadataKind.ImageSubtitle)
                            {
                                tracks.SetPresentationMode((uint)i, Windows.Media.Playback.TimedMetadataTrackPresentationMode.Hidden);
                                ActivateSubtitleTrack(track);
                                break;
                            }
                        }
                    }
                    ShowOsd("Subtitles: Enabled");
                }
                else
                {
                    ActivateSubtitleTrack(null!);
                    ShowOsd("Subtitles: Disabled");
                }
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
            var playbackItemSource = _nativePlayer?.Source as Windows.Media.Playback.MediaPlaybackItem;
            if (playbackItemSource != null)
            {
                var tracks = playbackItemSource.TimedMetadataTracks;
                if (tracks.Count > 0)
                {
                    for (int i = 0; i < tracks.Count; i++)
                    {
                        var track = tracks[i];
                        if (track.TimedMetadataKind == Windows.Media.Core.TimedMetadataKind.Subtitle || 
                            track.TimedMetadataKind == Windows.Media.Core.TimedMetadataKind.ImageSubtitle)
                        {
                            var trackIndex = i;
                            var lang = string.IsNullOrWhiteSpace(track.Language) ? "Unknown" : GetFriendlyLanguageName(track.Language);

                            // Show the original label exactly as named in the file; fall back only if empty
                            var displayName = !string.IsNullOrWhiteSpace(track.Label) ? track.Label : $"Track {trackIndex + 1}";

                            var trackItem = new ToggleMenuFlyoutItem 
                            { 
                                Text = displayName,
                                IsChecked = track == _activeSubtitleTrack
                            };
                            trackItem.Click += (s, a) =>
                            {
                                // Disable previous selected tracks first
                                for (int j = 0; j < tracks.Count; j++)
                                {
                                    tracks.SetPresentationMode((uint)j, Windows.Media.Playback.TimedMetadataTrackPresentationMode.Disabled);
                                }
                                // Enable this track as Hidden so it raises events
                                tracks.SetPresentationMode((uint)trackIndex, Windows.Media.Playback.TimedMetadataTrackPresentationMode.Hidden);
                                ActivateSubtitleTrack(track);
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

        private static string GetFriendlyLanguageName(string isoCode)
        {
            if (string.IsNullOrWhiteSpace(isoCode)) return "Unknown";
            try
            {
                // Try getting culture name from ISO 639-1 or ISO 639-2 codes
                var culture = new System.Globalization.CultureInfo(isoCode);
                return culture.DisplayName;
            }
            catch
            {
                // Hardcode fallbacks for common media language codes if CultureInfo fails
                var clean = isoCode.Trim().ToLowerInvariant();
                switch (clean)
                {
                    case "eng": case "en": return "English";
                    case "jpn": case "ja": return "Japanese";
                    case "spa": case "es": return "Spanish";
                    case "fre": case "fra": case "fr": return "French";
                    case "ger": case "deu": case "de": return "German";
                    case "chi": case "zho": case "zh": return "Chinese";
                    case "rus": case "ru": return "Russian";
                    case "kor": case "ko": return "Korean";
                    case "por": case "pt": return "Portuguese";
                    case "ita": case "it": return "Italian";
                    default: return isoCode; // Fallback to raw code
                }
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
                        var lang = string.IsNullOrWhiteSpace(track.Language) ? "Unknown" : GetFriendlyLanguageName(track.Language);
                        var label = string.IsNullOrWhiteSpace(track.Label) ? $"Audio {trackIndex + 1}" : track.Label;
                        
                        var displayName = (label.Equals(track.Language, StringComparison.OrdinalIgnoreCase) || label.StartsWith("Audio Track", StringComparison.OrdinalIgnoreCase)) 
                            ? lang 
                            : $"{lang} ({label})";

                        var trackItem = new ToggleMenuFlyoutItem 
                        { 
                            Text = displayName,
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
                    if (e.NewValue > 100)
                    {
                        ShowOsd($"Volume: {e.NewValue:0}% (Amplified)");
                    }
                    else
                    {
                        ShowOsd($"Volume: {e.NewValue:0}%");
                    }
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
                newVolume = Math.Max(0, Math.Min(200, newVolume));
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
                if (SubtitleTextBlock != null)
                {
                    SubtitleTextBlock.FontSize = e.NewValue;
                }
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

        private void ActivateSubtitleTrack(Windows.Media.Core.TimedMetadataTrack track)
        {
            if (_activeSubtitleTrack != null)
            {
                _activeSubtitleTrack.CueEntered -= ActiveTrack_CueEntered;
                _activeSubtitleTrack.CueExited -= ActiveTrack_CueExited;
            }

            _activeSubtitleTrack = track;

            if (_activeSubtitleTrack != null)
            {
                _activeSubtitleTrack.CueEntered += ActiveTrack_CueEntered;
                _activeSubtitleTrack.CueExited += ActiveTrack_CueExited;
                
                // Render already active cue if present
                if (_activeSubtitleTrack.ActiveCues.Count > 0 && _activeSubtitleTrack.ActiveCues[0] is Windows.Media.Core.TimedTextCue cue)
                {
                    RenderSubtitleCue(cue);
                }
            }
            else
            {
                if (SubtitleBackgroundBorder != null) SubtitleBackgroundBorder.Visibility = Visibility.Collapsed;
                if (SubtitleTextBlock != null) SubtitleTextBlock.Text = "";
            }
        }

        private void ActiveTrack_CueEntered(Windows.Media.Core.TimedMetadataTrack sender, Windows.Media.Core.MediaCueEventArgs args)
        {
            if (args.Cue is Windows.Media.Core.TimedTextCue cue)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    RenderSubtitleCue(cue);
                });
            }
        }

        private void ActiveTrack_CueExited(Windows.Media.Core.TimedMetadataTrack sender, Windows.Media.Core.MediaCueEventArgs args)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (SubtitleBackgroundBorder != null) SubtitleBackgroundBorder.Visibility = Visibility.Collapsed;
                if (SubtitleTextBlock != null) SubtitleTextBlock.Text = "";
            });
        }

        private void RenderSubtitleCue(Windows.Media.Core.TimedTextCue cue)
        {
            if (SubtitleTextBlock == null || SubtitleBackgroundBorder == null) return;

            var lines = new System.Collections.Generic.List<string>();
            foreach (var line in cue.Lines)
            {
                if (!string.IsNullOrEmpty(line.Text))
                {
                    lines.Add(line.Text);
                }
            }

            if (lines.Count > 0)
            {
                SubtitleTextBlock.Text = string.Join(Environment.NewLine, lines);
                SubtitleBackgroundBorder.Visibility = Visibility.Visible;
            }
            else
            {
                SubtitleTextBlock.Text = "";
                SubtitleBackgroundBorder.Visibility = Visibility.Collapsed;
            }
        }

        private void VideoSurface_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
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
                    ShowOsd("Exit Fullscreen");
                }
                else
                {
                    win.AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);
                    if (bar != null) bar.Visibility = Visibility.Collapsed;
                    ShowOsd("Fullscreen");
                }
            }
        }
    }
}
