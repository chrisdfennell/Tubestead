namespace Tubestead.Infrastructure.Settings;

/// <summary>Canonical setting keys plus their built-in defaults and the
/// environment variable that may override the default. This registry is the
/// single source of truth for "what is configurable" and drives both the
/// effective-value resolver and the admin settings screen.</summary>
public static class SettingKeys
{
    // ---- First-run / branding ----
    public const string SetupCompleted = "setup.completed";
    public const string SiteName = "site.name";
    public const string SiteLogoPath = "site.logoPath";
    public const string PublicUrl = "site.publicUrl";

    // ---- Storage ----
    public const string MediaPath = "media.path";

    // ---- Uploads ----
    public const string MaxUploadBytes = "uploads.maxBytes";
    public const string AllowedExtensions = "uploads.allowedExtensions";

    // ---- Transcoding ----
    /// <summary>"original-only" (default) or "auto-renditions".</summary>
    public const string TranscodeMode = "transcode.mode";
    public const string TranscodePresets = "transcode.presets";

    // ---- Access ----
    public const string RegistrationOpen = "registration.open";

    /// <summary>Definition of every known setting. <c>Default</c> is the fallback,
    /// <c>EnvVar</c> (if set) overrides the default when no DB value exists, and
    /// <c>Secret</c> marks values that must never be returned to the client.</summary>
    public static readonly IReadOnlyList<SettingDefinition> Definitions =
    [
        new(SetupCompleted,    "false",                                            null,                     IsBool: true),
        new(SiteName,          "Tubestead",                                        "TUBESTEAD_SITE_NAME"),
        new(SiteLogoPath,      "",                                                 null),
        new(PublicUrl,         "",                                                 "TUBESTEAD_PUBLIC_URL"),
        new(MediaPath,         "/media",                                           "TUBESTEAD_MEDIA_PATH"),
        new(MaxUploadBytes,    (50L * 1024 * 1024 * 1024).ToString(),              "TUBESTEAD_MAX_UPLOAD_BYTES"),
        new(AllowedExtensions, ".mp4,.mov,.mkv,.webm,.avi,.m4v,.ts",               "TUBESTEAD_ALLOWED_EXTENSIONS"),
        new(TranscodeMode,     "original-only",                                    "TUBESTEAD_TRANSCODE_MODE"),
        new(TranscodePresets,  "1080p,720p,480p",                                  "TUBESTEAD_TRANSCODE_PRESETS"),
        new(RegistrationOpen,  "false",                                            "TUBESTEAD_REGISTRATION_OPEN", IsBool: true),
    ];

    public static readonly IReadOnlyDictionary<string, SettingDefinition> ByKey =
        Definitions.ToDictionary(d => d.Key, StringComparer.OrdinalIgnoreCase);
}

/// <param name="Key">Dotted setting key.</param>
/// <param name="Default">Built-in fallback value.</param>
/// <param name="EnvVar">Optional environment variable that overrides the default.</param>
/// <param name="IsBool">Hint for typed parsing / UI rendering.</param>
/// <param name="Secret">If true, never exposed through the API.</param>
public record SettingDefinition(
    string Key,
    string Default,
    string? EnvVar,
    bool IsBool = false,
    bool Secret = false);
