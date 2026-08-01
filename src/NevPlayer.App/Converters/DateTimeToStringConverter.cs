using System;
using Microsoft.UI.Xaml.Data;

namespace NevPlayer.App.Converters
{
    /// <summary>
    /// Converts a DateTime to a friendly relative string (e.g. "Today", "Yesterday", "3 days ago").
    /// </summary>
    public class DateTimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTime dt)
            {
                if (dt == DateTime.MinValue) return string.Empty;

                var now = DateTime.Now;
                var diff = now - dt;

                if (diff.TotalMinutes < 1)
                    return "Just now";
                if (diff.TotalHours < 1)
                    return $"{(int)diff.TotalMinutes}m ago";
                if (diff.TotalHours < 24 && dt.Date == now.Date)
                    return $"Today at {dt:h:mm tt}";
                if (diff.TotalDays < 2 && dt.Date == now.Date.AddDays(-1))
                    return $"Yesterday at {dt:h:mm tt}";
                if (diff.TotalDays < 7)
                    return $"{(int)diff.TotalDays} days ago";
                if (diff.TotalDays < 30)
                    return $"{(int)(diff.TotalDays / 7)} week{((int)(diff.TotalDays / 7) > 1 ? "s" : "")} ago";

                return dt.ToString("MMM d, yyyy");
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
