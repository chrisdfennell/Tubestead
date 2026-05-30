using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Tubestead.Domain;
using Tubestead.Domain.Entities;
using Tubestead.Infrastructure.Data;
using Tubestead.Infrastructure.Processing;
using Xunit;

namespace Tubestead.Tests;

/// <summary>Lifecycle behaviour of the M2 processing stub: a queued video becomes
/// Ready; an unknown id is a safe no-op.</summary>
public class VideoProcessorTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TubesteadDbContext _db;
    private readonly VideoProcessor _processor;

    public VideoProcessorTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<TubesteadDbContext>().UseSqlite(_connection).Options;
        _db = new TubesteadDbContext(options);
        _db.Database.EnsureCreated();
        _processor = new VideoProcessor(_db, NullLogger<VideoProcessor>.Instance);
    }

    [Fact]
    public async Task Processing_moves_video_to_ready()
    {
        var video = new Video { Title = "Barn raising", Status = VideoStatus.Processing };
        _db.Videos.Add(video);
        await _db.SaveChangesAsync();

        await _processor.ProcessAsync(video.Id);

        var reloaded = await _db.Videos.AsNoTracking().FirstAsync(v => v.Id == video.Id);
        Assert.Equal(VideoStatus.Ready, reloaded.Status);
        Assert.Null(reloaded.StatusMessage);
    }

    [Fact]
    public async Task Unknown_video_is_a_noop()
    {
        // Should not throw.
        await _processor.ProcessAsync(Guid.NewGuid());
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
