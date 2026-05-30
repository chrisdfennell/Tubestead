namespace Tubestead.Domain.Entities;

/// <summary>A single video the owner uploaded. The original file is always kept;
/// transcoded renditions (HLS, etc.) are produced on demand and tracked in
/// <see cref="MediaRendition"/>.</summary>
public class Video
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Id of the <c>ApplicationUser</c> who uploaded it. Stored as a raw
    /// Guid so the Domain layer stays free of any Identity/EF dependency.</summary>
    public Guid OwnerId { get; set; }

    public VideoStatus Status { get; set; } = VideoStatus.Uploading;
    /// <summary>Human-readable detail for the current status (e.g. a failure reason).</summary>
    public string? StatusMessage { get; set; }

    // ---- Probed metadata (populated once processing runs in M3) ----
    public double? DurationSeconds { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }

    // ---- Original source file ----
    /// <summary>Path to the stored original, relative to the media root.</summary>
    public string? OriginalFilePath { get; set; }
    /// <summary>The filename the user uploaded, preserved for the download flow.</summary>
    public string? OriginalFileName { get; set; }
    public long? OriginalSizeBytes { get; set; }

    /// <summary>Path to the generated poster image, relative to the media root.</summary>
    public string? ThumbnailPath { get; set; }

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<MediaRendition> Renditions { get; set; } = new List<MediaRendition>();
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    public ICollection<PlaylistItem> PlaylistItems { get; set; } = new List<PlaylistItem>();
}
