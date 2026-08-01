using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NevPlayer.Core.Models;
using NevPlayer.Core.Services;

namespace NevPlayer.App.Views
{
    public sealed partial class HistoryPage : Page
    {
        private readonly IPlaybackHistoryService? _historyService;
        private readonly IPlaybackService? _playbackService;

        public ObservableCollection<HistoryItem> HistoryItems { get; } = new ObservableCollection<HistoryItem>();

        public HistoryPage()
        {
            this.InitializeComponent();
            _historyService = App.HistoryService;
            _playbackService = App.PlaybackService;

            Loaded += HistoryPage_Loaded;
        }

        private void HistoryPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadHistory();
        }

        private void LoadHistory()
        {
            HistoryItems.Clear();

            if (_historyService == null) return;

            var items = _historyService.GetRecentlyPlayed(50);
            foreach (var item in items)
            {
                HistoryItems.Add(item);
            }

            EmptyState.Visibility = HistoryItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            HistoryListView.Visibility = HistoryItems.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void HistoryListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is HistoryItem historyItem)
            {
                var mediaItem = new MediaItem
                {
                    FilePath = historyItem.FilePath,
                    Title = historyItem.Title,
                    AlbumArtPath = historyItem.ArtworkPath
                };

                _playbackService?.ClearQueue();
                _playbackService?.Enqueue(mediaItem);

                if (this.Frame != null)
                {
                    this.Frame.Navigate(typeof(CinemaPage));
                }
            }
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            // Clear all history items from the view
            // (The underlying service stores in memory only for prototype phase)
            HistoryItems.Clear();
            EmptyState.Visibility = Visibility.Visible;
            HistoryListView.Visibility = Visibility.Collapsed;
        }
    }
}
