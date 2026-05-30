namespace Tubestead.Domain.Entities;

/// <summary>A playable/downloadable variant of a <see cref="Video"/>: the original
/// itself, a transcoded MP4, or an HLS rendition at a given resolution.</summary>
public class MediaRendition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid VideoId { get; set; }
    public Video? Video { get; set; }

    /// <summary>Short display label, e.g. "Original", "1080p", "720p".</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Container/format, e.g. "mp4" or "hls".</summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>Path to the file (mp4) or playlist (.m3u8), relative to the media root.</summary>
    public string Path { get; set; } = string.Empty;

    public int? Width { get; set; }
    public int? Height { get; set; }
    public long? SizeBytes { get; set; }

    /// <summary>True for the untouched source file the user uploaded.</summary>
    public bool IsOriginal { get; set; }

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
