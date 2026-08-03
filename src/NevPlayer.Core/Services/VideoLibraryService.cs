using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NevPlayer.Core.Models;

namespace NevPlayer.Core.Services
{
    public class VideoLibraryService : IVideoLibraryService
    {
        private readonly List<MediaItem> _library = new List<MediaItem>();
        private readonly string _libraryFilePath;
        private readonly object _lock = new object();
        private readonly IVideoThumbnailService _thumbnailService;
        private readonly IMetadataExtractorService _metadataExtractorService;

        public VideoLibraryService(IVideoThumbnailService thumbnailService, IMetadataExtractorService metadataExtractorService)
        {
            _thumbnailService = thumbnailService;
            _metadataExtractorService = metadataExtractorService;

            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var directory = Path.Combine(appDataPath, "NevPlayer");
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            
            _libraryFilePath = Path.Combine(directory, "library_videos.json");
            LoadLibrary();
        }

        private void LoadLibrary()
        {
            try
            {
                if (File.Exists(_libraryFilePath))
                {
                    var json = File.ReadAllText(_libraryFilePath);
                    var items = System.Text.Json.JsonSerializer.Deserialize<List<MediaItem>>(json);
                    if (items != null)
                    {
                        lock (_library)
                        {
                            _library.AddRange(items);
                        }
                    }
                }
            }
            catch
            {
                // Ignore load errors
            }
        }

        private async Task SaveLibraryAsync()
        {
            List<MediaItem> snapshot;
            lock (_library)
            {
                snapshot = _library.ToList();
            }

            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(snapshot, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_libraryFilePath, json);
            }
            catch
            {
                // Ignore save errors
            }
        }

        public IReadOnlyList<MediaItem> GetAllVideos()
        {
            lock (_library)
            {
                return _library.ToList().AsReadOnly();
            }
        }

        public async Task AddVideoFilesAsync(IEnumerable<string> filePaths, string albumName = "Other Videos")
        {
            if (filePaths == null || !filePaths.Any()) return;

            var newItems = new List<MediaItem>();

            foreach (var file in filePaths)
            {
                // Skip if already in library
                bool exists;
                lock (_library)
                {
                    exists = _library.Any(m => string.Equals(m.FilePath, file, StringComparison.OrdinalIgnoreCase));
                }
                if (exists) continue;

                var item = new MediaItem
                {
                    FilePath = file,
                    Title = Path.GetFileNameWithoutExtension(file),
                    Album = albumName
                };

                // Extract thumbnail
                if (_thumbnailService != null)
                {
                    try
                    {
                        item.AlbumArtPath = await _thumbnailService.GetThumbnailAsync(file);
                    }
                    catch
                    {
                        // Ignore thumbnail errors
                    }
                }

                // Extract metadata
                if (_metadataExtractorService != null)
                {
                    await _metadataExtractorService.ExtractMetadataAsync(item);
                }

                newItems.Add(item);
            }

            lock (_library)
            {
                _library.AddRange(newItems);
            }

            await SaveLibraryAsync();
        }

        public async Task AddVideoFolderAsync(string folderPath)
        {
            if (!Directory.Exists(folderPath)) return;

            var albumName = new DirectoryInfo(folderPath).Name;
            var files = new List<string>();

            var extensions = new[] { ".mp4", ".mkv", ".avi", ".webm", ".mov", ".wmv", ".flv", ".m4v", ".ts", ".vob", ".3gp", ".mpeg", ".mpg", ".m2ts", ".divx", ".xvid", ".rmvb", ".asf" };

            try
            {
                var allFiles = Directory.GetFiles(folderPath);
                foreach (var file in allFiles)
                {
                    var ext = Path.GetExtension(file).ToLowerInvariant();
                    if (extensions.Contains(ext))
                    {
                        files.Add(file);
                    }
                }
            }
            catch
            {
                // Ignore access errors
            }

            await AddVideoFilesAsync(files, albumName);
        }

        public void RemoveVideoFile(string filePath)
        {
            bool removed = false;
            lock (_library)
            {
                var item = _library.FirstOrDefault(m => string.Equals(m.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
                if (item != null)
                {
                    _library.Remove(item);
                    removed = true;
                }
            }
            if (removed)
            {
                _ = SaveLibraryAsync();
            }
        }

        public void ClearLibrary()
        {
            lock (_library)
            {
                _library.Clear();
            }
            _ = SaveLibraryAsync();
        }
    }
}
