using Microsoft.UI.Xaml;

namespace NevPlayer.App
{
    public partial class App : Application
    {
        public App()
        {
            this.InitializeComponent();
            this.UnhandledException += App_UnhandledException;
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"[NevPlayer AppDomain UnhandledException] {e.ExceptionObject}");
            };
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"[NevPlayer TaskScheduler UnobservedTaskException] {e.Exception}");
                e.SetObserved();
            };
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[NevPlayer UnhandledException] {e.Message} \n {e.Exception}");
            e.Handled = true; // Prevent app crash
        }

        // Static Service Locators for prototype phase
        public static NevPlayer.Core.Services.IPlaybackService? PlaybackService { get; private set; }
        public static NevPlayer.Core.Services.IMediaLibraryService? LibraryService { get; private set; }
        public static NevPlayer.Core.Services.IPlaybackHistoryService? HistoryService { get; private set; }
        public static NevPlayer.Core.Services.IVideoLibraryService? VideoLibraryService { get; private set; }
        public static NevPlayer.Core.Services.IMetadataExtractorService? MetadataExtractorService { get; private set; }
        public static NevPlayer.Core.Services.IFavoritesService? FavoritesService { get; private set; }
        public static NevPlayer.Core.Services.ISettingsService? SettingsService { get; private set; }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                HistoryService = new NevPlayer.Core.Services.PlaybackHistoryService();
                FavoritesService = new NevPlayer.Core.Services.FavoritesService();
                SettingsService = new NevPlayer.Core.Services.SettingsService();

                var mediaEngine = new NevPlayer.App.Services.SwitchableMediaPlayer(SettingsService);
                PlaybackService = new NevPlayer.Core.Services.PlaybackService(mediaEngine, HistoryService);
                
                // Set default speed if saved
                PlaybackService.PlaybackSpeed = SettingsService.DefaultPlaybackSpeed;

                MetadataExtractorService = new NevPlayer.App.Services.WindowsMetadataExtractorService();
                LibraryService = new NevPlayer.Core.Services.MediaLibraryService();
                
                var thumbnailService = new NevPlayer.App.Services.WindowsThumbnailService();
                VideoLibraryService = new NevPlayer.Core.Services.VideoLibraryService(thumbnailService, MetadataExtractorService);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NevPlayer Service Init Error] {ex}");
            }

            m_window = new MainWindow();
            m_window.Activate();

            if (SettingsService != null)
            {
                ApplyTheme(SettingsService.AppTheme);
            }


        }

        private void ApplyTheme(string theme)
        {
            if (m_window?.Content is FrameworkElement rootElement)
            {
                if (theme == "Dark")
                    rootElement.RequestedTheme = ElementTheme.Dark;
                else if (theme == "Light")
                    rootElement.RequestedTheme = ElementTheme.Light;
                else
                    rootElement.RequestedTheme = ElementTheme.Default;
            }
        }

        public void ApplyCurrentTheme()
        {
            ApplyTheme(SettingsService?.AppTheme ?? "Dark");
        }

        public Window? MainWindow => m_window;
        private Window? m_window;
    }
}
