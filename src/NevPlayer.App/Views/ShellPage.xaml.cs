using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Linq;

namespace NevPlayer.App.Views
{
    public sealed partial class ShellPage : Page
    {
        private readonly NevPlayer.Core.Services.IPlaybackService? _playbackService;

        public ShellPage()
        {
            this.InitializeComponent();
            _playbackService = App.PlaybackService;
            
            // Set default selected item
            NavView.SelectedItem = NavView.MenuItems[0];
            
            ContentFrame.Navigated += ContentFrame_Navigated;

            // Explicitly navigate on load — NavigationView.SelectionChanged does NOT
            // fire when SelectedItem is set programmatically before the control renders.
            Loaded += ShellPage_Loaded;
        }

        private void ShellPage_Loaded(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(typeof(HomePage));
        }

        public Microsoft.UI.Xaml.UIElement AppTitleBarElement => AppTitleBar;

        private void ContentFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
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
                if (tag == "Home")
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
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var query = sender.Text.ToLowerInvariant();
                var videos = App.VideoLibraryService?.GetAllVideos() ?? System.Array.Empty<NevPlayer.Core.Models.MediaItem>();
                var music = App.LibraryService?.GetAllMedia() ?? System.Array.Empty<NevPlayer.Core.Models.MediaItem>();
                
                var results = videos.Concat(music)
                    .Where(m => m.Title != null && m.Title.ToLowerInvariant().Contains(query))
                    .Take(5)
                    .Select(m => m.Title)
                    .ToList();
                
                sender.ItemsSource = results;
            }
        }

        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
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
                }
            }
        }
    }
}
