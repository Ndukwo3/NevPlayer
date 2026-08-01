using System.Threading.Tasks;

namespace NevPlayer.Core.Services
{
    public interface IVideoThumbnailService
    {
        Task<string> GetThumbnailAsync(string videoFilePath);
    }
}
