namespace NevPlayer.Core.Models
{
    public class MediaTrackInfo
    {
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
