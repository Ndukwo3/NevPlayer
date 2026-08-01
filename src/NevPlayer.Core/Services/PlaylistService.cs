using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using NevPlayer.Core.Models;

namespace NevPlayer.Core.Services
{
    public class PlaylistService : IPlaylistService
    {
        private readonly string _playlistDirectory;

        public PlaylistService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _playlistDirectory = Path.Combine(appDataPath, "NevPlayer", "Playlists");
            
            if (!Directory.Exists(_playlistDirectory))
            {
                Directory.CreateDirectory(_playlistDirectory);
            }
        }

        public async Task SavePlaylistAsync(string name, IEnumerable<MediaItem> items)
        {
            var filePath = Path.Combine(_playlistDirectory, $"{name}.json");
            var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);
        }

        public async Task<List<MediaItem>> LoadPlaylistAsync(string name)
        {
            var filePath = Path.Combine(_playlistDirectory, $"{name}.json");
            if (File.Exists(filePath))
            {
                var json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<List<MediaItem>>(json) ?? new List<MediaItem>();
            }
            return new List<MediaItem>();
        }
    }
}
