using Hangfire;
using Tubestead.Infrastructure.Processing;

namespace Tubestead.Api.Jobs;

/// <summary>Hangfire-backed queue: enqueues <see cref="IVideoProcessor.ProcessAsync"/>
/// so it runs on a background worker with Hangfire's retry/visibility.</summary>
public class HangfireVideoJobQueue(IBackgroundJobClient client) : IVideoJobQueue
{
    public void EnqueueProcessing(Guid videoId) =>
        client.Enqueue<IVideoProcessor>(p => p.ProcessAsync(videoId, CancellationToken.None));
}
