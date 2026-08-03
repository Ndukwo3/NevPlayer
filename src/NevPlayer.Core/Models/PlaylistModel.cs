using System;

namespace NevPlayer.Core.Models
{
    public class PlaylistModel
    {
        public string Name { get; set; } = string.Empty;
        public string ArtworkPath { get; set; } = string.Empty;
        public bool HasArtwork => !string.IsNullOrEmpty(ArtworkPath);
    }
}
