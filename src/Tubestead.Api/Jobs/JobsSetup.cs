using System;
using Hangfire;
using Hangfire.Storage.SQLite;
using Tubestead.Infrastructure.Processing;

namespace Tubestead.Api.Jobs;

public static class JobsSetup
{
    /// <summary>Registers the background job system. In the Testing environment this
    /// is a no-op queue (no server, no storage) so integration tests stay fast and
    /// deterministic; otherwise it's Hangfire backed by a local SQLite file.</summary>
    public static void AddTubesteadJobs(this WebApplicationBuilder builder, string dataPath)
    {
        if (builder.Environment.IsEnvironment("Testing"))
        {
            builder.Services.AddSingleton<IVideoJobQueue, NoOpVideoJobQueue>();
            return;
        }

        var hangfireDb = Path.Combine(dataPath, "hangfire.db");
        builder.Services.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSQLiteStorage(hangfireDb, new SQLiteStorageOptions
            {
                // Snappier pickup of queued processing jobs (default is 15s).
                QueuePollInterval = TimeSpan.FromSeconds(2),
            }));

        // Conservative worker count — transcoding is CPU-heavy on NAS hardware.
        // M3 ties this to the transcode-concurrency setting.
        builder.Services.AddHangfireServer(o => o.WorkerCount = 2);
        builder.Services.AddScoped<IVideoJobQueue, HangfireVideoJobQueue>();
    }
}
