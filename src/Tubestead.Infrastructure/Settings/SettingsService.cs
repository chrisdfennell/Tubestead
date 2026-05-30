using Microsoft.EntityFrameworkCore;
using Tubestead.Domain.Entities;
using Tubestead.Infrastructure.Data;

namespace Tubestead.Infrastructure.Settings;

/// <inheritdoc />
public class SettingsService(TubesteadDbContext db) : ISettingsService
{
    public async Task<string> GetStringAsync(string key, CancellationToken ct = default)
    {
        var def = Definition(key);

        var stored = await db.AppSettings
            .Where(s => s.Key == key)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct);
        if (stored is not null)
            return stored;

        if (def.EnvVar is { } env &&
            Environment.GetEnvironmentVariable(env) is { Length: > 0 } envValue)
            return envValue;

        return def.Default;
    }

    public async Task<bool> GetBoolAsync(string key, CancellationToken ct = default)
    {
        var raw = await GetStringAsync(key, ct);
        return raw.Equals("true", StringComparison.OrdinalIgnoreCase) || raw == "1";
    }

    public async Task<long> GetLongAsync(string key, CancellationToken ct = default)
    {
        var raw = await GetStringAsync(key, ct);
        return long.TryParse(raw, out var value) ? value : 0L;
    }

    public Task<bool> IsSetupCompletedAsync(CancellationToken ct = default) =>
        GetBoolAsync(SettingKeys.SetupCompleted, ct);

    public async Task<IReadOnlyDictionary<string, string>> GetAllEffectiveAsync(CancellationToken ct = default)
    {
        var stored = await db.AppSettings.ToDictionaryAsync(s => s.Key, s => s.Value, ct);
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var def in SettingKeys.Definitions)
        {
            if (def.Secret)
                continue;

            if (stored.TryGetValue(def.Key, out var v) && v is not null)
                result[def.Key] = v;
            else if (def.EnvVar is { } env &&
                     Environment.GetEnvironmentVariable(env) is { Length: > 0 } envValue)
                result[def.Key] = envValue;
            else
                result[def.Key] = def.Default;
        }

        return result;
    }

    public async Task SetAsync(string key, string? value, CancellationToken ct = default)
    {
        _ = Definition(key); // reject unknown keys
        await UpsertAsync(key, value, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task SetManyAsync(IReadOnlyDictionary<string, string?> values, CancellationToken ct = default)
    {
        foreach (var (key, value) in values)
        {
            _ = Definition(key);
            await UpsertAsync(key, value, ct);
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task UpsertAsync(string key, string? value, CancellationToken ct)
    {
        var existing = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == key, ct);
        if (existing is null)
            db.AppSettings.Add(new AppSetting { Key = key, Value = value, UpdatedUtc = DateTimeOffset.UtcNow });
        else
        {
            existing.Value = value;
            existing.UpdatedUtc = DateTimeOffset.UtcNow;
        }
    }

    private static SettingDefinition Definition(string key) =>
        SettingKeys.ByKey.TryGetValue(key, out var def)
            ? def
            : throw new ArgumentException($"Unknown setting key '{key}'.", nameof(key));
}
