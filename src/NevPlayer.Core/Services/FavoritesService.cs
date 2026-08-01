using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NevPlayer.Core.Models;

namespace NevPlayer.Core.Services
{
    public class FavoritesService : IFavoritesService
    {
        private readonly List<MediaItem> _favorites = new List<MediaItem>();

        public event EventHandler? FavoritesChanged;

        public IReadOnlyList<MediaItem> GetFavorites()
        {
            return _favorites.AsReadOnly();
        }

        public Task AddFavoriteAsync(MediaItem item)
        {
            if (item != null && !_favorites.Any(f => f.FilePath == item.FilePath))
            {
                _favorites.Add(item);
                FavoritesChanged?.Invoke(this, EventArgs.Empty);
            }
            return Task.CompletedTask;
        }

        public Task RemoveFavoriteAsync(MediaItem item)
        {
            var existing = _favorites.FirstOrDefault(f => f.FilePath == item.FilePath);
            if (existing != null)
            {
                _favorites.Remove(existing);
                FavoritesChanged?.Invoke(this, EventArgs.Empty);
            }
            return Task.CompletedTask;
        }

        public bool IsFavorite(MediaItem item)
        {
            return item != null && _favorites.Any(f => f.FilePath == item.FilePath);
        }
    }
}
