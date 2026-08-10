using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NevPlayer.Core.Models
{
    public class MediaItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; } = TimeSpan.Zero;
        
        private bool _isPlaying = false;
        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (_isPlaying != value)
                {
                    _isPlaying = value;
                    OnPropertyChanged();
                }
            }
        }
        
        // Metadata fields
        public string Artist { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public string Resolution { get; set; } = string.Empty;
        public string CodecInfo { get; set; } = string.Empty;
        public string AlbumArtPath { get; set; } = string.Empty;
        
        // New Metadata fields
        public uint Bitrate { get; set; }
        public ulong FileSize { get; set; }
        public string Genre { get; set; } = string.Empty;
        public uint Year { get; set; }

        // Helper properties for UI binding
        public bool IsVideo
        {
            get
            {
                if (string.IsNullOrEmpty(FilePath)) return false;
                var ext = System.IO.Path.GetExtension(FilePath).ToLowerInvariant();
                var videoExtensions = new System.Collections.Generic.HashSet<string>
                {
                    ".mp4", ".mkv", ".avi", ".webm", ".mov", ".wmv", ".flv", ".m4v",
                    ".ts", ".m2ts", ".vob", ".3gp", ".mpeg", ".mpg", ".divx",
                    ".xvid", ".rmvb", ".asf"
                };
                return videoExtensions.Contains(ext);
            }
        }

        // Helper properties for UI binding
        public string FormattedDuration => Duration.ToString(Duration.Hours > 0 ? @"hh\:mm\:ss" : @"mm\:ss");
        public string DisplayMetadata 
        {
            get 
            {
                var parts = new System.Collections.Generic.List<string>();
                if (!string.IsNullOrEmpty(Resolution)) parts.Add(Resolution);
                if (!string.IsNullOrEmpty(FormattedDuration) && Duration.TotalSeconds > 0) parts.Add(FormattedDuration);
                return string.Join(" • ", parts);
            }
        }
    }
}
