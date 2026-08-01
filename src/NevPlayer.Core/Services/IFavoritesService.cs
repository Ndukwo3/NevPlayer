using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NevPlayer.Core.Models;

namespace NevPlayer.Core.Services
{
    public interface IFavoritesService
    {
        IReadOnlyList<MediaItem> GetFavorites();
        Task AddFavoriteAsync(MediaItem item);
        Task RemoveFavoriteAsync(MediaItem item);
        bool IsFavorite(MediaItem item);
        event EventHandler? FavoritesChanged;
    }
}
