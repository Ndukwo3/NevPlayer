using Microsoft.UI.Xaml.Data;
using NevPlayer.Core.Models;
using System;

namespace NevPlayer.App.Converters
{
    public class ItemIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is MediaItem mediaItem)
            {
                var queue = App.PlaybackService?.Queue;
                if (queue != null)
                {
                    int index = -1;
                    for (int i = 0; i < queue.Count; i++)
                    {
                        if (queue[i] == mediaItem)
                        {
                            index = i;
                            break;
                        }
                    }
                    if (index >= 0)
                    {
                        return $"{(index + 1):D2}.";
                    }
                }
            }
            return "00.";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
