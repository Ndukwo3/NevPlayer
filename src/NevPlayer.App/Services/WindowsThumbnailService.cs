using System;
using System.IO;
using System.Threading.Tasks;
using NevPlayer.Core.Services;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace NevPlayer.App.Services
{
    public class WindowsThumbnailService : IVideoThumbnailService
    {
        private readonly string _cacheDirectory;

        public WindowsThumbnailService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _cacheDirectory = Path.Combine(appDataPath, "NevPlayer", "Thumbnails");
            if (!Directory.Exists(_cacheDirectory))
            {
                Directory.CreateDirectory(_cacheDirectory);
            }
        }

        public async Task<string> GetThumbnailAsync(string videoFilePath)
        {
            try
            {
                var hash = GetStringHash(videoFilePath);
                var cachedPath = Path.Combine(_cacheDirectory, $"{hash}.jpg");

                if (File.Exists(cachedPath))
                {
                    return cachedPath;
                }

                var file = await StorageFile.GetFileFromPathAsync(videoFilePath);
                using var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.VideosView, 400, ThumbnailOptions.UseCurrentScale);

                if (thumbnail != null)
                {
                    using var fileStream = File.Create(cachedPath);
                    var inputStream = thumbnail.AsStreamForRead();
                    await inputStream.CopyToAsync(fileStream);
                    return cachedPath;
                }

                // Fallback to MediaClip if standard OS thumbnail provider fails (common for some MP4 files)
                var clip = await Windows.Media.Editing.MediaClip.CreateFromFileAsync(file);
                var composition = new Windows.Media.Editing.MediaComposition();
                composition.Clips.Add(clip);
                
                var imageStream = await composition.GetThumbnailAsync(TimeSpan.Zero, 400, 0, Windows.Media.Editing.VideoFramePrecision.NearestFrame);
                if (imageStream != null)
                {
                    using var fileStream = File.Create(cachedPath);
                    var inputStream = imageStream.AsStreamForRead();
                    await inputStream.CopyToAsync(fileStream);
                    return cachedPath;
                }
            }
            catch
            {
                // Ignore extraction failures
            }

            return string.Empty; // Return empty string if no thumbnail could be extracted
        }

        private string GetStringHash(string text)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            var hash = md5.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
