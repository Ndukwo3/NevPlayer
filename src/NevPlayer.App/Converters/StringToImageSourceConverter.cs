using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;

namespace NevPlayer.App.Converters
{
    public class StringToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string path && !string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    Uri uri;
                    // Check if it's already a valid URI scheme
                    if (path.StartsWith("ms-appx", StringComparison.OrdinalIgnoreCase) || 
                        path.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWith("file", StringComparison.OrdinalIgnoreCase))
                    {
                        uri = new Uri(path);
                    }
                    else
                    {
                        // Assume it's a local file path
                        uri = new Uri("file:///" + path.Replace("\\", "/"));
                    }
                    return new BitmapImage(uri);
                }
                catch
                {
                    return null!;
                }
            }
            return null!;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
