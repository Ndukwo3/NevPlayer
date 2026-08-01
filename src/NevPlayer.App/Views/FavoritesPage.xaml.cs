using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NevPlayer.Core.Models;
using NevPlayer.Core.Services;

namespace NevPlayer.App.Views
{
    public sealed partial class FavoritesPage : Page
    {
        private ObservableCollection<MediaItem> _favorites = new ObservableCollection<MediaItem>();

        public FavoritesPage()
        {
            this.InitializeComponent();
            FavoritesGrid.ItemsSource = _favorites;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            LoadFavorites();
            
            // In a real app with DI, you'd inject IFavoritesService. We are using App static locator.
            // If FavoritesService is added to App.xaml.cs:
            if (App.FavoritesService != null)
            {
                App.FavoritesService.FavoritesChanged += FavoritesService_FavoritesChanged;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (App.FavoritesService != null)
            {
                App.FavoritesService.FavoritesChanged -= FavoritesService_FavoritesChanged;
            }
        }

        private void FavoritesService_FavoritesChanged(object? sender, System.EventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                LoadFavorites();
            });
        }

        private void LoadFavorites()
        {
            if (App.FavoritesService == null) return;
            
            _favorites.Clear();
            var favs = App.FavoritesService.GetFavorites();
            foreach (var f in favs)
            {
                _favorites.Add(f);
            }
            
            EmptyStateText.Visibility = _favorites.Count == 0 ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
        }

        private void FavoritesGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is MediaItem item)
            {
                App.PlaybackService?.PlayQueueItem(0); // This is just a stub. Normally we'd set the queue and play.
                App.PlaybackService?.ClearQueue();
                App.PlaybackService?.Enqueue(item);
                App.PlaybackService?.PlayQueueItem(0);
                // Simple heuristic: if it's in the video list, it's a video
                var videos = App.VideoLibraryService?.GetAllVideos() ?? System.Array.Empty<NevPlayer.Core.Models.MediaItem>();
                if (videos.Any(v => v.FilePath == item.FilePath))
                {
                    this.Frame.Navigate(typeof(CinemaPage));
                }
            }
        }
    }
}
