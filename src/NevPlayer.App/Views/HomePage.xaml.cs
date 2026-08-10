using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls;
using NevPlayer.Core.Models;
using NevPlayer.Core.Services;

namespace NevPlayer.App.Views
{
    public sealed partial class HomePage : Page
    {
        private readonly IPlaybackHistoryService? _historyService;
        private readonly IPlaybackService? _playbackService;
        private readonly IVideoLibraryService? _videoLibraryService;
        
        public ObservableCollection<HistoryItem> RecentItems { get; } = new ObservableCollection<HistoryItem>();

        public HomePage()
        {
            this.InitializeComponent();
            
            _historyService = App.HistoryService;
            _playbackService = App.PlaybackService;
            _videoLibraryService = App.VideoLibraryService;

            LoadHistory();
        }

        private void LoadHistory()
        {
            if (_historyService == null) return;
            
            RecentItems.Clear();
            var history = _historyService.GetRecentlyPlayed(20); // Get top 20
            foreach (var item in history)
            {
                RecentItems.Add(item);
            }
        }

        private void RecentGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is HistoryItem historyItem)
            {
                System.Diagnostics.Debug.WriteLine("[LOG] ContinueWatching item clicked");
                // Play it directly via the engine
                var mediaItem = new MediaItem 
                { 
                    FilePath = historyItem.FilePath,
                    Title = historyItem.Title,
                    AlbumArtPath = historyItem.ArtworkPath
                };

                // Add to queue and play immediately
                _playbackService?.ClearQueue();
                
                if (_videoLibraryService != null)
                {
                    try
                    {
                        var allVideos = _videoLibraryService.GetAllVideos();
                        var matchedVideo = System.Linq.Enumerable.FirstOrDefault(allVideos, v => string.Equals(v.FilePath, historyItem.FilePath, StringComparison.OrdinalIgnoreCase));
                        
                        if (matchedVideo != null)
                        {
                            var siblings = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Where(allVideos, v => v.Album == matchedVideo.Album));
                            if (siblings.Count > 1)
                            {
                                foreach (var s in siblings)
                                {
                                    _playbackService?.Enqueue(s, autoPlay: false);
                                }
                                var targetIndex = siblings.IndexOf(matchedVideo);
                                _playbackService?.PlayQueueItem(targetIndex);
                            }
                            else
                            {
                                _playbackService?.Enqueue(matchedVideo, autoPlay: true);
                            }
                        }
                        else
                        {
                            _playbackService?.Enqueue(mediaItem, autoPlay: true);
                        }
                    }
                    catch
                    {
                        _playbackService?.Enqueue(mediaItem, autoPlay: true);
                    }
                }
                else
                {
                    _playbackService?.Enqueue(mediaItem, autoPlay: true);
                }
                // PlaybackService will automatically fetch the resume position when it loads.
                // We just need to navigate back to the Cinema view
                if (this.Frame != null)
                {
                    System.Diagnostics.Debug.WriteLine("[LOG] Navigation started");
                    this.Frame.Navigate(typeof(CinemaPage));
                }
            }
        }
        private async void ClearHistory_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_historyService != null)
            {
                await _historyService.ClearHistoryAsync();
            }
            RecentItems.Clear();
        }
    }
}
