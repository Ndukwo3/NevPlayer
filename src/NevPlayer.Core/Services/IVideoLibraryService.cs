using System.Collections.Generic;
using System.Threading.Tasks;
using NevPlayer.Core.Models;

namespace NevPlayer.Core.Services
{
    public interface IVideoLibraryService
    {
        IReadOnlyList<MediaItem> GetAllVideos();
        Task AddVideoFilesAsync(IEnumerable<string> filePaths, string albumName = "Other Videos");
        Task AddVideoFolderAsync(string folderPath);
        void RemoveVideoFile(string filePath);
    }
}
