using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace NevPlayer.App.Views
{
    public sealed partial class TestVideoPage : Page
    {
        private MediaPlayer _player;
        private MediaPlaybackItem? _playbackItem;

        public TestVideoPage()
        {
            this.InitializeComponent();
            _player = new MediaPlayer();
            
            // Set up diagnostic events on the player
            _player.MediaOpened += _player_MediaOpened;
            _player.MediaFailed += _player_MediaFailed;
            _player.MediaEnded += _player_MediaEnded;
            
            // We use SetMediaPlayer here to test if binding works properly in isolation
            VideoSurface.SetMediaPlayer(_player);
        }

        private void _player_MediaEnded(MediaPlayer sender, object args)
        {
            DispatcherQueue.TryEnqueue(() => StatusText.Text = "Status: Media Ended");
        }

        private void _player_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            Debug.WriteLine($"[TestPage] MediaFailed: {args.ErrorMessage}");
            DispatcherQueue.TryEnqueue(() => StatusText.Text = $"Status: Error - {args.ErrorMessage}");
        }

        private void _player_MediaOpened(MediaPlayer sender, object args)
        {
            Debug.WriteLine("[TestPage] MediaOpened triggered.");
            Debug.WriteLine($"[TestPage] Width: {_player.PlaybackSession.NaturalVideoWidth}");
            Debug.WriteLine($"[TestPage] Height: {_player.PlaybackSession.NaturalVideoHeight}");
            
            if (_playbackItem != null)
            {
                Debug.WriteLine($"[TestPage] VideoTracks Count: {_playbackItem.VideoTracks.Count}");
            }

            DispatcherQueue.TryEnqueue(() => 
            {
                StatusText.Text = $"Status: Opened ({_player.PlaybackSession.NaturalVideoWidth}x{_player.PlaybackSession.NaturalVideoHeight})";
                // Force an update layout to ensure dimensions apply
                VideoSurface.UpdateLayout();
            });
        }

        private async void LoadVideo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FileOpenPicker();
                
                // Get the current window's HWND by picking it from the App
                var app = App.Current as App;
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(app?.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                picker.ViewMode = PickerViewMode.List;
                picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
                picker.FileTypeFilter.Add(".mp4");
                picker.FileTypeFilter.Add(".mkv");
                picker.FileTypeFilter.Add(".avi");
                picker.FileTypeFilter.Add(".mov");

                StorageFile file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    StatusText.Text = $"Status: Loading {file.Name}...";
                    var source = MediaSource.CreateFromStorageFile(file);
                    _playbackItem = new MediaPlaybackItem(source);
                    _player.Source = _playbackItem;
                    _player.Play();
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Status: Exception - {ex.Message}";
                Debug.WriteLine($"[TestPage] Exception: {ex}");
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            _player.Play();
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            _player.Pause();
        }
    }
}
