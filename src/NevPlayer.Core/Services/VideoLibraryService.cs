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
        private readonly IVideoThumbnailService _thumbnailService;
        private readonly IMetadataExtractorService _metadataExtractorService;

        public VideoLibraryService(IVideoThumbnailService thumbnailService, IMetadataExtractorService metadataExtractorService)
        {
            _thumbnailService = thumbnailService;
            _metadataExtractorService = metadataExtractorService;
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
            lock (_library)
            {
                var item = _library.FirstOrDefault(m => string.Equals(m.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
                if (item != null)
                {
                    _library.Remove(item);
                }
            }
        }
    }
}
