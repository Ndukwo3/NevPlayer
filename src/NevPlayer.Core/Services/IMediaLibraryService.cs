using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NevPlayer.Core.Models;

namespace NevPlayer.Core.Services
{
    public interface IMediaLibraryService
    {
        IReadOnlyList<MediaItem> GetAllMedia();
        IReadOnlyList<string> GetAllArtists();
        IReadOnlyList<string> GetAllAlbums();
        IReadOnlyList<MediaItem> GetMediaByArtist(string artist);
        IReadOnlyList<MediaItem> GetMediaByAlbum(string album);
        Task AddMediaFilesAsync(IEnumerable<string> filePaths);
        Task AddMediaFolderAsync(string folderPath);
        void RemoveMediaFile(string filePath);
    }
}
