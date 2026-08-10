using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;

namespace NevPlayer.App
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            // Setup basic Window settings
            Title = "NevPlayer";
            ExtendsContentIntoTitleBar = true; // Use custom title bar
            AppWindow.SetIcon(System.IO.Path.Combine(System.AppContext.BaseDirectory, "Assets", "NevPlayer.ico"));

            // Set the custom title bar element so the system drag region 
            // doesn't overlap the hamburger menu button.
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            SetTitleBar(Shell.AppTitleBarElement);

            // Set dark theme default
            if (Content is FrameworkElement fe)
            {
                fe.RequestedTheme = ElementTheme.Dark;
            }
        }
    }
}
