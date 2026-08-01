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
        
        public ObservableCollection<HistoryItem> RecentItems { get; } = new ObservableCollection<HistoryItem>();

        public HomePage()
        {
            this.InitializeComponent();
            
            _historyService = App.HistoryService;
            _playbackService = App.PlaybackService;

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
                // Play it directly via the engine
                var mediaItem = new MediaItem 
                { 
                    FilePath = historyItem.FilePath,
                    Title = historyItem.Title,
                    AlbumArtPath = historyItem.ArtworkPath
                };

                // Add to queue and play immediately
                _playbackService?.ClearQueue();
                _playbackService?.Enqueue(mediaItem);
                
                // PlaybackService will automatically fetch the resume position when it loads.
                // We just need to navigate back to the Cinema view
                if (this.Frame != null)
                {
                    this.Frame.Navigate(typeof(CinemaPage));
                }
            }
        }
    }
}
