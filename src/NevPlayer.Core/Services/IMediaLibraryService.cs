using System.Collections.Generic;
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
        void RemoveMediaFile(string filePath);
    }
}
