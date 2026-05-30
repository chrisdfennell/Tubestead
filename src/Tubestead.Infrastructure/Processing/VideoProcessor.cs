using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tubestead.Domain;
using Tubestead.Infrastructure.Data;

namespace Tubestead.Infrastructure.Processing;

/// <summary>M2 implementation: validates the original exists and marks the video
/// ready. The real ffmpeg work (probe, thumbnail, remux, HLS) lands in M3 and
/// will slot into <see cref="ProcessAsync"/> between the Processing and Ready
/// transitions.</summary>
public class VideoProcessor(TubesteadDbContext db, ILogger<VideoProcessor> logger) : IVideoProcessor
{
    public async Task ProcessAsync(Guid videoId, CancellationToken ct = default)
    {
        var video = await db.Videos.FirstOrDefaultAsync(v => v.Id == videoId, ct);
        if (video is null)
        {
            logger.LogWarning("Processing requested for unknown video {VideoId}", videoId);
            return;
        }

        try
        {
            video.Status = VideoStatus.Processing;
            video.StatusMessage = "Processing…";
            video.UpdatedUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);

            // --- M3 will do real work here (ffprobe metadata, thumbnail, remux, HLS). ---

            video.Status = VideoStatus.Ready;
            video.StatusMessage = null;
            video.UpdatedUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);

            logger.LogInformation("Video {VideoId} processed and ready", videoId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Processing failed for video {VideoId}", videoId);
            video.Status = VideoStatus.Failed;
            video.StatusMessage = ex.Message;
            video.UpdatedUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(CancellationToken.None);
            throw; // let the scheduler record/retry the failure
        }
    }
}
