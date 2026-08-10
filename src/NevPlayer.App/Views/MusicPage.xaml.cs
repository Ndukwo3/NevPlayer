using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NevPlayer.Core.Models;
using NevPlayer.Core.Services;
using System.Linq;

namespace NevPlayer.App.Views
{
    public sealed partial class MusicPage : Page
    {
        private readonly IMediaLibraryService? _libraryService;
        private readonly IPlaybackService? _playbackService;
        
        public ObservableCollection<MediaItem> MediaItems { get; } = new ObservableCollection<MediaItem>();

        private readonly IPlaylistService _playlistService = new PlaylistService();
        private string _currentTab = "Songs";

        private string _searchQuery = string.Empty;

        public MusicPage()
        {
            this.InitializeComponent();
            _libraryService = App.LibraryService;
            _playbackService = App.PlaybackService;

            LoadLibrary();
        }

        public void ApplySearchFilter(string query)
        {
            _searchQuery = query ?? string.Empty;
            
            // Reload the active tab view with the query applied
            if (_currentTab == "Songs")
            {
                LoadLibrary();
            }
            else if (_currentTab == "Playlists")
            {
                LoadPlaylists();
            }
            else if (_currentTab == "Albums")
            {
                LoadAlbums();
            }
            else if (_currentTab == "Artists")
            {
                LoadArtists();
            }
        }

        private void LoadLibrary()
        {
            if (_libraryService == null) return;
            MediaItems.Clear();
            
            var queryLower = _searchQuery.ToLowerInvariant();
            var items = _libraryService.GetAllMedia();
            
            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(_searchQuery) || 
                    (item.Title != null && item.Title.ToLowerInvariant().Contains(queryLower)) ||
                    (item.Artist != null && item.Artist.ToLowerInvariant().Contains(queryLower)) ||
                    (item.Album != null && item.Album.ToLowerInvariant().Contains(queryLower)))
                {
                    MediaItems.Add(item);
                }
            }
        }

        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is HyperlinkButton button && button.Tag is string tabTag)
            {
                _currentTab = tabTag;

                // Update active tab visual state
                SongsTab.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(tabTag == "Songs" ? Microsoft.UI.Colors.White : Microsoft.UI.Colors.Gray);
                SongsTab.FontWeight = tabTag == "Songs" ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal;

                PlaylistsTab.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(tabTag == "Playlists" ? Microsoft.UI.Colors.White : Microsoft.UI.Colors.Gray);
                PlaylistsTab.FontWeight = tabTag == "Playlists" ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal;

                AlbumsTab.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(tabTag == "Albums" ? Microsoft.UI.Colors.White : Microsoft.UI.Colors.Gray);
                AlbumsTab.FontWeight = tabTag == "Albums" ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal;

                ArtistsTab.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(tabTag == "Artists" ? Microsoft.UI.Colors.White : Microsoft.UI.Colors.Gray);
                ArtistsTab.FontWeight = tabTag == "Artists" ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal;

                // Toggle visibility grids
                MediaGridView.Visibility = tabTag == "Songs" ? Visibility.Visible : Visibility.Collapsed;
                PlaylistsGridView.Visibility = tabTag == "Playlists" ? Visibility.Visible : Visibility.Collapsed;
                GroupedGridView.Visibility = (tabTag == "Albums" || tabTag == "Artists") ? Visibility.Visible : Visibility.Collapsed;

                if (tabTag == "Playlists")
                {
                    LoadPlaylists();
                }
                else if (tabTag == "Albums")
                {
                    LoadAlbums();
                }
                else if (tabTag == "Artists")
                {
                    LoadArtists();
                }
            }
        }

        private void LoadPlaylists()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var playlistDir = System.IO.Path.Combine(appDataPath, "NevPlayer", "Playlists");
            var playlists = new System.Collections.Generic.List<PlaylistModel>();
            var queryLower = _searchQuery.ToLowerInvariant();

            if (System.IO.Directory.Exists(playlistDir))
            {
                var files = System.IO.Directory.GetFiles(playlistDir, "*.json");
                foreach (var file in files)
                {
                    var name = System.IO.Path.GetFileNameWithoutExtension(file);
                    
                    // Filter playlist by name
                    if (!string.IsNullOrEmpty(_searchQuery) && !name.ToLowerInvariant().Contains(queryLower))
                    {
                        continue;
                    }

                    var model = new PlaylistModel { Name = name };

                    try
                    {
                        var json = System.IO.File.ReadAllText(file);
                        var items = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<MediaItem>>(json);
                        if (items != null && items.Count > 0)
                        {
                            // Grab first item's album art as cover
                            var firstArt = items.Find(x => !string.IsNullOrEmpty(x.AlbumArtPath))?.AlbumArtPath;
                            if (!string.IsNullOrEmpty(firstArt))
                            {
                                model.ArtworkPath = firstArt;
                            }
                        }
                    }
                    catch
                    {
                        // Ignore read/json parsing failures
                    }

                    playlists.Add(model);
                }
            }

            PlaylistsGridView.ItemsSource = playlists;
        }

        private void LoadAlbums()
        {
            if (_libraryService != null)
            {
                var albums = _libraryService.GetAllAlbums();
                if (!string.IsNullOrEmpty(_searchQuery))
                {
                    var queryLower = _searchQuery.ToLowerInvariant();
                    albums = albums.Where(a => a != null && a.ToLowerInvariant().Contains(queryLower)).ToList();
                }
                GroupedGridView.ItemsSource = albums;
            }
        }

        private void LoadArtists()
        {
            if (_libraryService != null)
            {
                var artists = _libraryService.GetAllArtists();
                if (!string.IsNullOrEmpty(_searchQuery))
                {
                    var queryLower = _searchQuery.ToLowerInvariant();
                    artists = artists.Where(a => a != null && a.ToLowerInvariant().Contains(queryLower)).ToList();
                }
                GroupedGridView.ItemsSource = artists;
            }
        }

        private async void PlaylistsGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is PlaylistModel playlist)
            {
                var items = await _playlistService.LoadPlaylistAsync(playlist.Name);
                if (items != null && items.Count > 0)
                {
                    _playbackService?.ClearQueue();
                    foreach (var item in items)
                    {
                        _playbackService?.Enqueue(item);
                    }
                    _playbackService?.PlayQueueItem(0);
                    this.Frame?.Navigate(typeof(CinemaPage));
                }
            }
        }

        private void RemovePlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string playlistName)
            {
                _playlistService.DeletePlaylist(playlistName);
                LoadPlaylists();
            }
        }

        private void GroupedGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is string name && _libraryService != null)
            {
                System.Collections.Generic.IReadOnlyList<MediaItem> items;
                if (_currentTab == "Albums")
                {
                    items = _libraryService.GetMediaByAlbum(name);
                }
                else
                {
                    items = _libraryService.GetMediaByArtist(name);
                }

                if (items != null && items.Count > 0)
                {
                    _playbackService?.ClearQueue();
                    foreach (var item in items)
                    {
                        _playbackService?.Enqueue(item);
                    }
                    _playbackService?.PlayQueueItem(0);
                    this.Frame?.Navigate(typeof(CinemaPage));
                }
            }
        }

        private async void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FolderPicker();
            var window = (Microsoft.UI.Xaml.Application.Current as App)?.MainWindow;
            if (window != null)
            {
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hWnd);
            }
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary;
            picker.FileTypeFilter.Add("*");

            var folder = await picker.PickSingleFolderAsync();
            if (window != null)
            {
                window.Activate();
            }

            if (folder != null)
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                // Run off-thread to avoid freezing the UI
                await System.Threading.Tasks.Task.Run(() => _libraryService!.AddMediaFolderAsync(folder.Path));
                LoadLibrary();
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void AddMusic_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            var window = (Microsoft.UI.Xaml.Application.Current as App)?.MainWindow;
            if (window != null)
            {
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hWnd);
            }
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary;
            picker.FileTypeFilter.Add(".mp3");
            picker.FileTypeFilter.Add(".flac");
            picker.FileTypeFilter.Add(".wav");

            var files = await picker.PickMultipleFilesAsync();
            if (window != null)
            {
                window.Activate(); // Bring window back to foreground
            }

            if (files != null && files.Count > 0)
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                
                var filePaths = new System.Collections.Generic.List<string>();
                foreach (var file in files) filePaths.Add(file.Path);

                // Run off-thread to avoid freezing the UI
                await System.Threading.Tasks.Task.Run(() => _libraryService!.AddMediaFilesAsync(filePaths));
                LoadLibrary();

                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void RemoveMusic_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string filePath)
            {
                _libraryService?.RemoveMediaFile(filePath);
                LoadLibrary();
            }
        }

        private void MediaGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is MediaItem item)
            {
                _playbackService?.ClearQueue();
                
                var dir = System.IO.Path.GetDirectoryName(item.FilePath);
                var siblings = MediaItems.Where(m => System.IO.Path.GetDirectoryName(m.FilePath) == dir).ToList();
                
                if (siblings.Count > 1)
                {
                    foreach (var sibling in siblings)
                    {
                        _playbackService?.Enqueue(sibling, autoPlay: false);
                    }
                    int index = siblings.IndexOf(item);
                    if (index >= 0)
                    {
                        _playbackService?.PlayQueueItem(index);
                    }
                }
                else
                {
                    _playbackService?.Enqueue(item, autoPlay: true);
                }
                
                if (this.Frame != null)
                {
                    this.Frame.Navigate(typeof(CinemaPage));
                }
            }
        }
    }
}
