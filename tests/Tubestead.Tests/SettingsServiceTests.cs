using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tubestead.Infrastructure.Data;
using Tubestead.Infrastructure.Settings;
using Xunit;

namespace Tubestead.Tests;

/// <summary>Verifies the config precedence chain: stored value > env var > default.</summary>
public class SettingsServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TubesteadDbContext _db;
    private readonly SettingsService _settings;

    public SettingsServiceTests()
    {
        // A real (in-memory) SQLite db, kept alive by holding the connection open.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<TubesteadDbContext>()
            .UseSqlite(_connection)
            .Options;
        _db = new TubesteadDbContext(options);
        _db.Database.EnsureCreated();
        _settings = new SettingsService(_db);
    }

    [Fact]
    public async Task Returns_builtin_default_when_nothing_set()
    {
        Assert.Equal("Tubestead", await _settings.GetStringAsync(SettingKeys.SiteName));
        Assert.False(await _settings.IsSetupCompletedAsync());
    }

    [Fact]
    public async Task Env_var_overrides_default()
    {
        await WithEnv("TUBESTEAD_SITE_NAME", "From Env", async () =>
            Assert.Equal("From Env", await _settings.GetStringAsync(SettingKeys.SiteName)));
    }

    [Fact]
    public async Task Stored_value_overrides_env_var_and_default()
    {
        await _settings.SetAsync(SettingKeys.SiteName, "From Db");
        await WithEnv("TUBESTEAD_SITE_NAME", "From Env", async () =>
            Assert.Equal("From Db", await _settings.GetStringAsync(SettingKeys.SiteName)));
    }

    [Fact]
    public async Task Bool_and_long_typed_accessors_parse()
    {
        await _settings.SetAsync(SettingKeys.SetupCompleted, "true");
        Assert.True(await _settings.GetBoolAsync(SettingKeys.SetupCompleted));

        await _settings.SetAsync(SettingKeys.MaxUploadBytes, "123456");
        Assert.Equal(123456, await _settings.GetLongAsync(SettingKeys.MaxUploadBytes));
    }

    [Fact]
    public async Task Unknown_key_is_rejected()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _settings.SetAsync("not.a.real.key", "x"));
    }

    private static async Task WithEnv(string name, string value, Func<Task> body)
    {
        var prev = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);
        try { await body(); }
        finally { Environment.SetEnvironmentVariable(name, prev); }
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
