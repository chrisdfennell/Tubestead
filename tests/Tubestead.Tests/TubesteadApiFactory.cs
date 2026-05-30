using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace Tubestead.Tests;

/// <summary>Spins up the real API against a throwaway SQLite file and temp media
/// directory, so each test run starts from a clean, isolated instance.</summary>
public class TubesteadApiFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "tubestead-tests", Guid.NewGuid().ToString("N"));

    public string MediaPath => Path.Combine(_root, "media");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_root);
        builder.UseEnvironment("Development");
        builder.UseSetting("TUBESTEAD_DATA_PATH", _root);
        builder.UseSetting("TUBESTEAD_DB_CONNECTION", $"Data Source={Path.Combine(_root, "test.db")}");
        builder.UseSetting("TUBESTEAD_MEDIA_PATH", MediaPath);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true); }
        catch { /* best effort cleanup */ }
    }
}
