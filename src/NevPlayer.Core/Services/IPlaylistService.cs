using System.Collections.Generic;
using System.Threading.Tasks;
using NevPlayer.Core.Models;

namespace NevPlayer.Core.Services
{
    public interface IPlaylistService
    {
        Task SavePlaylistAsync(string name, IEnumerable<MediaItem> items);
        Task<List<MediaItem>> LoadPlaylistAsync(string name);
        void DeletePlaylist(string name);
    }
}
