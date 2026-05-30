namespace Tubestead.Domain.Entities;

/// <summary>An ordered collection of videos (a simple "channel"/series).</summary>
public class Playlist
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Guid OwnerId { get; set; }

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<PlaylistItem> Items { get; set; } = new List<PlaylistItem>();
}

/// <summary>Join row giving a <see cref="Video"/> an ordered position within a
/// <see cref="Playlist"/>.</summary>
public class PlaylistItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PlaylistId { get; set; }
    public Playlist? Playlist { get; set; }

    public Guid VideoId { get; set; }
    public Video? Video { get; set; }

    /// <summary>Zero-based sort order within the playlist.</summary>
    public int Position { get; set; }
}
