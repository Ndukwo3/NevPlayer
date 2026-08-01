using System;

namespace NevPlayer.Core.Models
{
    public class HistoryItem
    {
        public string FilePath { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public TimeSpan LastPosition { get; set; } = TimeSpan.Zero;
        public TimeSpan Duration { get; set; } = TimeSpan.Zero;
        public DateTime LastPlayed { get; set; } = DateTime.MinValue;

        // Optionally store artwork if it's a video thumbnail or music album art
        public string ArtworkPath { get; set; } = string.Empty;
    }
}
