using System;
using System.IO;
using System.Threading.Tasks;
using NevPlayer.Core.Models;
using NevPlayer.Core.Services;
using Windows.Storage;

namespace NevPlayer.App.Services
{
    public class WindowsMetadataExtractorService : IMetadataExtractorService
    {
        public async Task ExtractMetadataAsync(MediaItem item)
        {
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(item.FilePath);
                var basicProps = await file.GetBasicPropertiesAsync();
                
                item.FileSize = basicProps.Size;

                var ext = Path.GetExtension(item.FilePath).ToLowerInvariant();
                var videoExtensions = new System.Collections.Generic.HashSet<string>
                {
                    ".mp4", ".mkv", ".avi", ".webm", ".mov", ".wmv", ".flv", ".m4v",
                    ".ts", ".m2ts", ".vob", ".3gp", ".mpeg", ".mpg", ".divx",
                    ".xvid", ".rmvb", ".asf"
                };
                var isVideo = videoExtensions.Contains(ext);

                if (isVideo)
                {
                    var videoProps = await file.Properties.GetVideoPropertiesAsync();
                    
                    if (videoProps != null)
                    {
                        if (videoProps.Duration != TimeSpan.Zero) item.Duration = videoProps.Duration;
                        if (videoProps.Width > 0 && videoProps.Height > 0) item.Resolution = $"{videoProps.Width}x{videoProps.Height}";
                        item.Bitrate = videoProps.Bitrate;
                        
                        // Keep using the file name for video titles to prevent generic/duplicate metadata tags from hiding it
                        item.Title = Path.GetFileNameWithoutExtension(item.FilePath);
                        if (videoProps.Year > 0) item.Year = videoProps.Year;
                    }
                }
                else
                {
                    var musicProps = await file.Properties.GetMusicPropertiesAsync();
                    if (musicProps != null)
                    {
                        if (musicProps.Duration != TimeSpan.Zero) item.Duration = musicProps.Duration;
                        if (!string.IsNullOrEmpty(musicProps.Title)) item.Title = musicProps.Title;
                        if (!string.IsNullOrEmpty(musicProps.Artist)) item.Artist = musicProps.Artist;
                        if (!string.IsNullOrEmpty(musicProps.Album)) item.Album = musicProps.Album;
                        if (musicProps.Year > 0) item.Year = musicProps.Year;
                        if (musicProps.Genre.Count > 0) item.Genre = string.Join(", ", musicProps.Genre);
                        item.Bitrate = musicProps.Bitrate;
                    }
                }
            }
            catch
            {
                // Ignore extraction failures, file might be locked or unsupported
            }
        }
    }
}
