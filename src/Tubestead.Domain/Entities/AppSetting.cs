namespace Tubestead.Domain.Entities;

/// <summary>A single persisted configuration value, editable from the setup
/// wizard and the admin UI. These take precedence over environment variables,
/// which in turn override built-in defaults.</summary>
public class AppSetting
{
    /// <summary>Stable dotted key, e.g. "site.name" or "uploads.maxBytes".</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Raw string value; typed accessors live in the settings service.</summary>
    public string? Value { get; set; }

    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
