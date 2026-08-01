using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NevPlayer.Core.Models;
using TagLib;

namespace NevPlayer.Core.Services
{
    public class MediaLibraryService : IMediaLibraryService
    {
        private readonly List<MediaItem> _library = new List<MediaItem>();
        private readonly string _artCacheDirectory;

        public MediaLibraryService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _artCacheDirectory = Path.Combine(appDataPath, "NevPlayer", "AlbumArt");
            
            if (!System.IO.Directory.Exists(_artCacheDirectory))
            {
                System.IO.Directory.CreateDirectory(_artCacheDirectory);
            }
        }

        public IReadOnlyList<MediaItem> GetAllMedia() => _library.AsReadOnly();

        public IReadOnlyList<string> GetAllArtists()
        {
            return _library
                .Select(m => m.Artist)
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Distinct()
                .OrderBy(a => a)
                .ToList();
        }

        public IReadOnlyList<string> GetAllAlbums()
        {
            return _library
                .Select(m => m.Album)
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Distinct()
                .OrderBy(a => a)
                .ToList();
        }

        public IReadOnlyList<MediaItem> GetMediaByArtist(string artist)
        {
            return _library.Where(m => string.Equals(m.Artist, artist, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public IReadOnlyList<MediaItem> GetMediaByAlbum(string album)
        {
            return _library.Where(m => string.Equals(m.Album, album, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public async Task AddMediaFilesAsync(IEnumerable<string> filePaths)
        {
            if (filePaths == null || !filePaths.Any()) return;

            await Task.Run(() =>
            {
                var newItems = new System.Collections.Concurrent.ConcurrentBag<MediaItem>();

                Parallel.ForEach(filePaths, file =>
                {
                    try
                    {
                        // Skip if already in library
                        if (_library.Any(m => m.FilePath == file)) return;

                        using var tfile = TagLib.File.Create(file);
                        var tag = tfile.Tag;
                        
                        var item = new MediaItem
                        {
                            FilePath = file,
                            Title = !string.IsNullOrWhiteSpace(tag.Title) ? tag.Title : Path.GetFileNameWithoutExtension(file),
                            Artist = tag.FirstPerformer ?? tag.FirstAlbumArtist ?? "Unknown Artist",
                            Album = tag.Album ?? "Unknown Album",
                            Duration = tfile.Properties.Duration,
                            Bitrate = (uint)tfile.Properties.AudioBitrate,
                            Year = tag.Year,
                            Genre = tag.FirstGenre ?? string.Empty,
                            FileSize = (ulong)new System.IO.FileInfo(file).Length
                        };

                        // Extract Album Art
                        if (tag.Pictures.Length > 0)
                        {
                            var pic = tag.Pictures[0];
                            var hash = GetStringHash(item.Album + item.Artist);
                            var artPath = Path.Combine(_artCacheDirectory, $"{hash}.jpg");

                            if (!System.IO.File.Exists(artPath))
                            {
                                System.IO.File.WriteAllBytes(artPath, pic.Data.Data);
                            }
                            item.AlbumArtPath = artPath;
                        }

                        newItems.Add(item);
                    }
                    catch (Exception)
                    {
                        // Skip files that TagLib cannot read
                    }
                });

                lock (_library)
                {
                    _library.AddRange(newItems);
                }
            });
        }

        public void RemoveMediaFile(string filePath)
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

        private string GetStringHash(string text)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            var hashBytes = md5.ComputeHash(bytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }
}
