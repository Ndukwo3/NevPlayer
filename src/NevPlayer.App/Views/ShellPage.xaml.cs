using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Linq;

namespace NevPlayer.App.Views
{
    public sealed partial class ShellPage : Page
    {
        private readonly NevPlayer.Core.Services.IPlaybackService? _playbackService;
        private bool _isDraggingSlider = false;

        public ShellPage()
        {
            this.InitializeComponent();
            _playbackService = App.PlaybackService;
            
            // Set default selected item
            NavView.SelectedItem = NavView.MenuItems[0];
            
            ContentFrame.Navigated += ContentFrame_Navigated;

            Loaded += ShellPage_Loaded;
            Unloaded += ShellPage_Unloaded;
        }

        private void ShellPage_Loaded(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(typeof(HomePage));

            if (_playbackService != null)
            {
                _playbackService.PositionChanged += PlaybackService_PositionChanged;
                _playbackService.StateChanged += PlaybackService_StateChanged;
                _playbackService.MediaChanged += PlaybackService_MediaChanged;
                _playbackService.Engine.DurationLoaded += Engine_DurationLoaded;

                SyncMiniPlayerState();
            }
        }

        private void ShellPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_playbackService != null)
            {
                _playbackService.PositionChanged -= PlaybackService_PositionChanged;
                _playbackService.StateChanged -= PlaybackService_StateChanged;
                _playbackService.MediaChanged -= PlaybackService_MediaChanged;
                _playbackService.Engine.DurationLoaded -= Engine_DurationLoaded;
            }
        }

        public Microsoft.UI.Xaml.UIElement AppTitleBarElement => AppTitleBar;

        public void SetFullscreenUI(bool isFullscreen)
        {
            if (isFullscreen)
            {
                AppTitleBar.Visibility = Visibility.Collapsed;
                NavView.IsPaneVisible = false;
                // Move NavView to top row and span all rows to take over the screen
                Grid.SetRow(NavView, 0);
                Grid.SetRowSpan(NavView, 3);
            }
            else
            {
                AppTitleBar.Visibility = Visibility.Visible;
                NavView.IsPaneVisible = true;
                // Restore NavView grid position (below TitleBar)
                Grid.SetRow(NavView, 1);
                Grid.SetRowSpan(NavView, 2);
            }
        }

        private void ContentFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            // Update the selected item in the NavigationView to match the page type we navigated to
            if (e.SourcePageType == typeof(CinemaPage))
            {
                if (NavView != null && NowPlayingNavItem != null)
                {
                    NavView.SelectedItem = NowPlayingNavItem;
                }
            }
            else if (e.SourcePageType == typeof(HomePage))
            {
                var homeItem = NavView?.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(x => x.Tag?.ToString() == "Home");
                if (homeItem != null) NavView!.SelectedItem = homeItem;
            }
            else if (e.SourcePageType == typeof(VideosPage))
            {
                var videosItem = NavView?.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(x => x.Tag?.ToString() == "Videos");
                if (videosItem != null) NavView!.SelectedItem = videosItem;
            }
            else if (e.SourcePageType == typeof(MusicPage))
            {
                var musicItem = NavView?.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(x => x.Tag?.ToString() == "Music");
                if (musicItem != null) NavView!.SelectedItem = musicItem;
            }
            else if (e.SourcePageType == typeof(FavoritesPage))
            {
                var favsItem = NavView?.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(x => x.Tag?.ToString() == "Favorites");
                if (favsItem != null) NavView!.SelectedItem = favsItem;
            }
            else if (e.SourcePageType == typeof(HistoryPage))
            {
                var histItem = NavView?.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(x => x.Tag?.ToString() == "History");
                if (histItem != null) NavView!.SelectedItem = histItem;
            }

            // The Mini Player should only be visible when we are NOT on the CinemaPage
            // and there is actual media loaded and ready/playing in the background.
            UpdateMiniPlayerVisibility();
        }

        private void UpdateMiniPlayerVisibility()
        {
            bool hasActiveMedia = _playbackService?.CurrentMedia != null;
            
            // Enable or disable the Now Playing side navigation item
            if (NowPlayingNavItem != null)
            {
                NowPlayingNavItem.IsEnabled = hasActiveMedia;
            }

            if (ContentFrame.CurrentSourcePageType == typeof(CinemaPage))
            {
                MiniPlayerDock.Visibility = Visibility.Collapsed;
            }
            else
            {
                MiniPlayerDock.Visibility = hasActiveMedia ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void SyncMiniPlayerState()
        {
            UpdateMiniPlayerVisibility();

            if (_playbackService?.CurrentMedia != null)
            {
                var media = _playbackService.CurrentMedia;
                MiniTitleText.Text = media.Title ?? System.IO.Path.GetFileNameWithoutExtension(media.FilePath);
                MiniArtistText.Text = !string.IsNullOrEmpty(media.Artist) ? media.Artist : "Unknown Artist";

                // Setup image art
                if (!string.IsNullOrEmpty(media.AlbumArtPath))
                {
                    MiniAlbumArt.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(media.AlbumArtPath));
                }
                else
                {
                    MiniAlbumArt.Source = null;
                }

                // Setup times & slider
                MiniTimelineSlider.Maximum = _playbackService.Duration.TotalSeconds;
                MiniTimelineSlider.Value = _playbackService.Position.TotalSeconds;
                MiniCurrentTimeText.Text = _playbackService.Position.ToString(@"mm\:ss");
                MiniTotalTimeText.Text = _playbackService.Duration.ToString(@"mm\:ss");

                // Sync Play/Pause Icon
                MiniPlayPauseButton.Content = _playbackService.State == NevPlayer.Core.Models.PlaybackState.Playing ? "\uE103" : "\uE102";
            }
        }

        private void PlaybackService_PositionChanged(object? sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (!_isDraggingSlider && MiniPlayerDock.Visibility == Visibility.Visible && _playbackService != null)
                {
                    MiniTimelineSlider.Value = _playbackService.Position.TotalSeconds;
                    MiniCurrentTimeText.Text = _playbackService.Position.ToString(@"mm\:ss");
                }
            });
        }

        private void PlaybackService_StateChanged(object? sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (_playbackService != null)
                {
                    MiniPlayPauseButton.Content = _playbackService.State == NevPlayer.Core.Models.PlaybackState.Playing ? "\uE103" : "\uE102";
                    UpdateMiniPlayerVisibility();
                }
            });
        }

        private void PlaybackService_MediaChanged(object? sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                SyncMiniPlayerState();
            });
        }

        private void Engine_DurationLoaded(object? sender, TimeSpan duration)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                MiniTimelineSlider.Maximum = duration.TotalSeconds;
                MiniTotalTimeText.Text = duration.ToString(@"mm\:ss");
            });
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                ContentFrame.Navigate(typeof(SettingsPage));
            }
            else if (args.SelectedItemContainer != null)
            {
                string? tag = args.SelectedItemContainer.Tag?.ToString();
                if (tag == "NowPlaying")
                {
                    ContentFrame.Navigate(typeof(CinemaPage));
                }
                else if (tag == "Home")
                {
                    ContentFrame.Navigate(typeof(HomePage));
                }
                else if (tag == "Videos")
                {
                    ContentFrame.Navigate(typeof(VideosPage));
                }
                else if (tag == "Music")
                {
                    ContentFrame.Navigate(typeof(MusicPage));
                }
                else if (tag == "Favorites")
                {
                    ContentFrame.Navigate(typeof(FavoritesPage));
                }
                else if (tag == "History")
                {
                    ContentFrame.Navigate(typeof(HistoryPage));
                }
            }
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            var query = sender.Text ?? string.Empty;

            // Update suggestions
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var queryLower = query.ToLowerInvariant();
                var videos = App.VideoLibraryService?.GetAllVideos() ?? System.Array.Empty<NevPlayer.Core.Models.MediaItem>();
                var music = App.LibraryService?.GetAllMedia() ?? System.Array.Empty<NevPlayer.Core.Models.MediaItem>();
                
                var results = videos.Concat(music)
                    .Where(m => m.Title != null && m.Title.ToLowerInvariant().Contains(queryLower))
                    .Take(5)
                    .Select(m => m.Title)
                    .ToList();
                
                sender.ItemsSource = results;
            }

            // Real-time filtering on the current page if it supports it
            if (ContentFrame.Content is MusicPage musicPage)
            {
                musicPage.ApplySearchFilter(query);
            }
            else if (ContentFrame.Content is VideosPage videosPage)
            {
                videosPage.ApplySearchFilter(query);
            }
        }

        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            string query = args.QueryText ?? string.Empty;

            if (args.ChosenSuggestion != null)
            {
                string? title = args.ChosenSuggestion.ToString();
                var videos = App.VideoLibraryService?.GetAllVideos() ?? System.Array.Empty<NevPlayer.Core.Models.MediaItem>();
                var music = App.LibraryService?.GetAllMedia() ?? System.Array.Empty<NevPlayer.Core.Models.MediaItem>();
                
                var item = videos.Concat(music).FirstOrDefault(m => m.Title == title);
                if (item != null)
                {
                    App.PlaybackService?.ClearQueue();
                    App.PlaybackService?.Enqueue(item);
                    App.PlaybackService?.PlayQueueItem(0);
                    
                    if (videos.Any(v => v.FilePath == item.FilePath))
                    {
                        ContentFrame.Navigate(typeof(CinemaPage));
                    }
                    return;
                }
            }

            // If user pressed enter, apply filter to the active view page
            if (ContentFrame.Content is MusicPage musicPage)
            {
                musicPage.ApplySearchFilter(query);
            }
            else if (ContentFrame.Content is VideosPage videosPage)
            {
                videosPage.ApplySearchFilter(query);
            }
        }

        private void MiniPlayerDock_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Only navigate back to CinemaPage if the user clicked the dock itself or non-button/non-slider text.
            // If they clicked Play/Pause or interacted with the progress bar, do not trigger page navigation.
            if (e.OriginalSource is DependencyObject source)
            {
                // Traverse up the tree to check if they clicked a Button or Slider
                DependencyObject? current = source;
                while (current != null && current != MiniPlayerDock)
                {
                    if (current is Button || current is Slider)
                    {
                        return; // Prevent navigating when operating controls
                    }
                    current = VisualTreeHelper.GetParent(current);
                }
            }

            // Sync the side menu selection visual highlight
            if (NavView != null && NowPlayingNavItem != null)
            {
                NavView.SelectedItem = NowPlayingNavItem;
            }

            ContentFrame.Navigate(typeof(CinemaPage));
        }

        private void MiniPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (_playbackService == null) return;

            if (_playbackService.State == NevPlayer.Core.Models.PlaybackState.Playing)
            {
                _playbackService.Pause();
            }
            else
            {
                _playbackService.Play();
            }
        }

        private void MiniSkipBackward_Click(object sender, RoutedEventArgs e)
        {
            if (_playbackService == null) return;
            var newPos = _playbackService.Position - TimeSpan.FromSeconds(10);
            if (newPos < TimeSpan.Zero) newPos = TimeSpan.Zero;
            _playbackService.Seek(newPos);
        }

        private void MiniSkipForward_Click(object sender, RoutedEventArgs e)
        {
            if (_playbackService == null) return;
            var newPos = _playbackService.Position + TimeSpan.FromSeconds(10);
            if (newPos > _playbackService.Duration) newPos = _playbackService.Duration;
            _playbackService.Seek(newPos);
        }

        private void MiniPrevious_Click(object sender, RoutedEventArgs e)
        {
            _playbackService?.Previous();
        }

        private void MiniNext_Click(object sender, RoutedEventArgs e)
        {
            _playbackService?.Next();
        }

        private void MiniTimelineSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            // Check for large manual seek jumps
            if (Math.Abs(e.NewValue - e.OldValue) > 1.5 && _playbackService != null)
            {
                _playbackService.Seek(TimeSpan.FromSeconds(e.NewValue));
            }
        }
    }
}
