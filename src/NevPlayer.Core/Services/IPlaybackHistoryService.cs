using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NevPlayer.Core.Models;

namespace NevPlayer.Core.Services
{
    public interface IPlaybackHistoryService
    {
        IReadOnlyList<HistoryItem> GetRecentlyPlayed(int count);
        TimeSpan GetResumePosition(string filePath);
        Task UpdateHistoryAsync(MediaItem item, TimeSpan position, TimeSpan duration);
        Task ClearHistoryAsync();
    }
}
