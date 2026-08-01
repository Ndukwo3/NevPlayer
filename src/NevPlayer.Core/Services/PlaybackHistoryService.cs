using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NevPlayer.Core.Models;

namespace NevPlayer.Core.Services
{
    public class PlaybackHistoryService : IPlaybackHistoryService
    {
        private readonly string _historyFilePath;
        private List<HistoryItem> _history = new List<HistoryItem>();
        private readonly object _lock = new object();

        public PlaybackHistoryService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var directory = Path.Combine(appDataPath, "NevPlayer");
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            
            _historyFilePath = Path.Combine(directory, "history.json");
            LoadHistory();
        }

        private void LoadHistory()
        {
            try
            {
                if (File.Exists(_historyFilePath))
                {
                    var json = File.ReadAllText(_historyFilePath);
                    _history = JsonSerializer.Deserialize<List<HistoryItem>>(json) ?? new List<HistoryItem>();
                }
            }
            catch
            {
                _history = new List<HistoryItem>();
            }
        }

        private async Task SaveHistoryAsync()
        {
            List<HistoryItem> snapshot;
            lock (_lock)
            {
                snapshot = _history.ToList();
            }

            try
            {
                var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_historyFilePath, json);
            }
            catch
            {
                // Ignore save errors for prototype
            }
        }

        public IReadOnlyList<HistoryItem> GetRecentlyPlayed(int count)
        {
            lock (_lock)
            {
                return _history.OrderByDescending(h => h.LastPlayed).Take(count).ToList().AsReadOnly();
            }
        }

        public TimeSpan GetResumePosition(string filePath)
        {
            lock (_lock)
            {
                var item = _history.FirstOrDefault(h => string.Equals(h.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
                // If they finished the video (e.g. less than 5 seconds remaining), don't resume, start from beginning.
                if (item != null)
                {
                    if (item.Duration.TotalSeconds > 0 && (item.Duration - item.LastPosition).TotalSeconds < 5)
                    {
                        return TimeSpan.Zero;
                    }
                    return item.LastPosition;
                }
                return TimeSpan.Zero;
            }
        }

        public async Task UpdateHistoryAsync(MediaItem item, TimeSpan position, TimeSpan duration)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.FilePath)) return;

            // Only track items longer than a few seconds
            if (duration.TotalSeconds < 10) return;

            lock (_lock)
            {
                var existing = _history.FirstOrDefault(h => string.Equals(h.FilePath, item.FilePath, StringComparison.OrdinalIgnoreCase));
                
                if (existing != null)
                {
                    existing.LastPosition = position;
                    existing.Duration = duration;
                    existing.LastPlayed = DateTime.Now;
                    
                    if (string.IsNullOrEmpty(existing.Title)) existing.Title = item.Title;
                    if (string.IsNullOrEmpty(existing.ArtworkPath)) existing.ArtworkPath = item.AlbumArtPath;
                }
                else
                {
                    _history.Add(new HistoryItem
                    {
                        FilePath = item.FilePath,
                        Title = item.Title,
                        LastPosition = position,
                        Duration = duration,
                        LastPlayed = DateTime.Now,
                        ArtworkPath = item.AlbumArtPath
                    });
                }
                
                // Keep history size manageable
                if (_history.Count > 100)
                {
                    _history = _history.OrderByDescending(h => h.LastPlayed).Take(100).ToList();
                }
            }

            await SaveHistoryAsync();
        }

        public async Task ClearHistoryAsync()
        {
            lock (_lock)
            {
                _history.Clear();
            }
            await SaveHistoryAsync();
        }
    }
}
