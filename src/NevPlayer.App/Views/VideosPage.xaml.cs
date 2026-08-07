using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NevPlayer.Core.Models;
using NevPlayer.Core.Services;
using NevPlayer.App.Services;

namespace NevPlayer.App.Views
{
    public class GroupInfoList<T> : ObservableCollection<T>, System.ComponentModel.INotifyPropertyChanged
    {
        public object? Key { get; set; }
        
        private string? _coverImagePath;
        public string? CoverImagePath
        {
            get => _coverImagePath;
            set
            {
                if (_coverImagePath != value)
                {
                    _coverImagePath = value;
                    OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(CoverImagePath)));
                }
            }
        }

        public GroupInfoList(System.Collections.Generic.IEnumerable<T> items) : base(items) { }
    }

    public sealed partial class VideosPage : Page
    {
        private readonly IVideoLibraryService? _videoLibraryService;
        private readonly IPlaybackService? _playbackService;
        
        public ObservableCollection<GroupInfoList<MediaItem>> GroupedItems { get; } = new ObservableCollection<GroupInfoList<MediaItem>>();

        public VideosPage()
        {
            this.InitializeComponent();
            _videoLibraryService = App.VideoLibraryService;
            _playbackService = App.PlaybackService;
        }

        private void VideosPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadLibrary();
        }

        private string _searchQuery = string.Empty;

        public void ApplySearchFilter(string query)
        {
            _searchQuery = query ?? string.Empty;
            LoadLibrary();
        }

        private void LoadLibrary()
        {
            if (_videoLibraryService == null) return;
            GroupedItems.Clear();
            var items = _videoLibraryService.GetAllVideos();
            
            // Filter list based on search query
            if (!string.IsNullOrEmpty(_searchQuery))
            {
                var queryLower = _searchQuery.ToLowerInvariant();
                items = items.Where(item => 
                    (item.Title != null && item.Title.ToLowerInvariant().Contains(queryLower)) ||
                    (item.Album != null && item.Album.ToLowerInvariant().Contains(queryLower)) ||
                    (item.Artist != null && item.Artist.ToLowerInvariant().Contains(queryLower))
                ).ToList();
            }
            
            var queryGrouped = from item in items
                               group item by item.Album into g
                               select new GroupInfoList<MediaItem>(g) 
                               { 
                                   Key = g.Key,
                                   CoverImagePath = g.FirstOrDefault(x => !string.IsNullOrEmpty(x.AlbumArtPath))?.AlbumArtPath 
                               };

            foreach (var g in queryGrouped)
            {
                GroupedItems.Add(g);
            }

            // Asynchronously generate/load thumbnails in the background
            LoadThumbnails(items);
        }

        private void LoadThumbnails(System.Collections.Generic.IEnumerable<MediaItem> items)
        {
            var thumbService = new WindowsThumbnailService();
            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item.AlbumArtPath))
                {
                    _ = System.Threading.Tasks.Task.Run(async () =>
                    {
                        var thumbPath = await thumbService.GetThumbnailAsync(item.FilePath);
                        if (!string.IsNullOrEmpty(thumbPath))
                        {
                            DispatcherQueue.TryEnqueue(() =>
                            {
                                item.AlbumArtPath = thumbPath;
                                
                                // Automatically refresh group covers
                                foreach (var group in GroupedItems)
                                {
                                    if (group.Contains(item))
                                    {
                                        group.CoverImagePath = group.FirstOrDefault(x => !string.IsNullOrEmpty(x.AlbumArtPath))?.AlbumArtPath;
                                        break;
                                    }
                                }
                            });
                        }
                    });
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            FolderGridView.Visibility = Visibility.Visible;
            VideoGridView.Visibility = Visibility.Collapsed;
            BackButton.Visibility = Visibility.Collapsed;
            PageTitle.Text = "Videos";
            PageSubtitle.Text = "Your local video collection";
            VideoGridView.ItemsSource = null;
        }

        private async void FolderGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is GroupInfoList<MediaItem> folder)
            {
                // Capture items into a standard collection to avoid WinUI COM Interop bugs with custom generic types
                var items = new ObservableCollection<MediaItem>(folder);
                var key = folder.Key?.ToString() ?? "Videos";
                var count = folder.Count;

                // Crucial: Wait briefly for the native WinUI click animation to finish 
                // before hiding the grid, otherwise it throws a severe Access Violation crash.
                await System.Threading.Tasks.Task.Delay(50);

                FolderGridView.Visibility = Visibility.Collapsed;
                
                VideoGridView.ItemsSource = items;
                VideoGridView.Visibility = Visibility.Visible;
                
                BackButton.Visibility = Visibility.Visible;
                
                PageTitle.Text = key;
                PageSubtitle.Text = $"{count} videos";
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
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
            picker.FileTypeFilter.Add("*");

            var folder = await picker.PickSingleFolderAsync();
            if (window != null)
            {
                window.Activate();
            }

            if (folder != null)
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                // Run the heavy I/O (metadata extraction, thumbnail generation) off the UI thread
                // to prevent the app from freezing during import.
                await System.Threading.Tasks.Task.Run(() => _videoLibraryService!.AddVideoFolderAsync(folder.Path));
                LoadLibrary();
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void AddVideos_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            var window = (Microsoft.UI.Xaml.Application.Current as App)?.MainWindow;
            if (window != null)
            {
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hWnd);
            }
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
            var supportedExtensions = new[] { ".mp4", ".mkv", ".avi", ".webm", ".mov", ".wmv", ".flv", ".m4v", ".ts", ".vob", ".3gp", ".mpeg", ".mpg", ".m2ts" };
            foreach (var ext in supportedExtensions)
            {
                picker.FileTypeFilter.Add(ext);
            }

            var files = await picker.PickMultipleFilesAsync();
            if (window != null)
            {
                window.Activate();
            }

            if (files != null && files.Count > 0)
            {
                LoadingOverlay.Visibility = Visibility.Visible;

                var filePaths = new System.Collections.Generic.List<string>();
                foreach (var file in files)
                {
                    filePaths.Add(file.Path);
                }

                // Run the heavy I/O (metadata extraction, thumbnail generation) off the UI thread
                // to prevent the app from freezing during import.
                await System.Threading.Tasks.Task.Run(() => _videoLibraryService!.AddVideoFilesAsync(filePaths));
                LoadLibrary();

                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void RemoveVideo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string filePath)
            {
                _videoLibraryService?.RemoveVideoFile(filePath);
                LoadLibrary();
            }
        }

        private void MediaGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is MediaItem item)
            {
                _playbackService?.ClearQueue();
                _playbackService?.Enqueue(item, autoPlay: false);
                
                if (this.Frame != null)
                {
                    this.Frame.Navigate(typeof(CinemaPage));
                }
            }
        }
        private void ClearVideos_Click(object sender, RoutedEventArgs e)
        {
            _videoLibraryService?.ClearLibrary();
            LoadLibrary();

            // Revert view to folder list if we were inside a folder
            FolderGridView.Visibility = Visibility.Visible;
            VideoGridView.Visibility = Visibility.Collapsed;
            BackButton.Visibility = Visibility.Collapsed;
            PageTitle.Text = "Videos";
            PageSubtitle.Text = "Your local video collection";
            VideoGridView.ItemsSource = null;
        }
    }
}
