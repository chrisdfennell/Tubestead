namespace Tubestead.Infrastructure.Settings;

/// <summary>Reads and writes application settings with the precedence:
/// stored value (wizard / admin UI)  >  environment variable  >  built-in default.</summary>
public interface ISettingsService
{
    /// <summary>Effective string value for a known key, applying the full precedence chain.</summary>
    Task<string> GetStringAsync(string key, CancellationToken ct = default);

    Task<bool> GetBoolAsync(string key, CancellationToken ct = default);
    Task<long> GetLongAsync(string key, CancellationToken ct = default);

    /// <summary>True once the setup wizard has been completed.</summary>
    Task<bool> IsSetupCompletedAsync(CancellationToken ct = default);

    /// <summary>Effective values for every known (non-secret) key — for the admin UI.</summary>
    Task<IReadOnlyDictionary<string, string>> GetAllEffectiveAsync(CancellationToken ct = default);

    /// <summary>Persists a value (admin UI / wizard). Unknown keys are rejected.</summary>
    Task SetAsync(string key, string? value, CancellationToken ct = default);

    /// <summary>Persists several values in one transaction.</summary>
    Task SetManyAsync(IReadOnlyDictionary<string, string?> values, CancellationToken ct = default);
}
