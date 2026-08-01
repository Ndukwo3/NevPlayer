using System.Threading.Tasks;
using NevPlayer.Core.Models;

namespace NevPlayer.Core.Services
{
    public interface IMetadataExtractorService
    {
        Task ExtractMetadataAsync(MediaItem item);
    }
}
