namespace Tubestead.Infrastructure.Processing;

/// <summary>Runs the post-upload pipeline for one video. In M2 this only moves the
/// video to <c>Ready</c>; M3 plugs in ffprobe metadata, thumbnail generation,
/// faststart remux, and optional HLS renditions here.</summary>
public interface IVideoProcessor
{
    Task ProcessAsync(Guid videoId, CancellationToken ct = default);
}

/// <summary>Enqueues processing work. Abstracts the background scheduler (Hangfire)
/// so upload handling and tests don't depend on it directly.</summary>
public interface IVideoJobQueue
{
    void EnqueueProcessing(Guid videoId);
}

/// <summary>No-op queue used in tests so no background server is required.</summary>
public sealed class NoOpVideoJobQueue : IVideoJobQueue
{
    public void EnqueueProcessing(Guid videoId) { }
}
