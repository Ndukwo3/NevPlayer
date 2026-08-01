using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NevPlayer.Core.Models;
using NevPlayer.Core.Services;

namespace NevPlayer.App.Views
{
    public sealed partial class MusicPage : Page
    {
        private readonly IMediaLibraryService? _libraryService;
        private readonly IPlaybackService? _playbackService;
        
        public ObservableCollection<MediaItem> MediaItems { get; } = new ObservableCollection<MediaItem>();

        public MusicPage()
        {
            this.InitializeComponent();
            
            // In a real app we'd inject these via DI, but for now we'll instantiate them or get from a locator.
            // Ideally _playbackService should be a singleton. Assuming App has a way to get it, or we create a simple static locator.
            // For now, we will just use basic instantiation if we can't get it from DI, but we need the same instance of PlaybackService.
            // Wait, we need to access the singleton PlaybackService used by CinemaPage.
            // A simple solution for this prototype is a static locator in App.xaml.cs.
            _libraryService = App.LibraryService;
            _playbackService = App.PlaybackService;

            LoadLibrary();
        }

        private void LoadLibrary()
        {
            if (_libraryService == null) return;
            MediaItems.Clear();
            foreach (var item in _libraryService.GetAllMedia())
            {
                MediaItems.Add(item);
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

                await _libraryService!.AddMediaFilesAsync(filePaths);
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
                _playbackService?.Enqueue(item);
                
                if (this.Frame != null)
                {
                    this.Frame.Navigate(typeof(CinemaPage));
                }
            }
        }
    }
}
