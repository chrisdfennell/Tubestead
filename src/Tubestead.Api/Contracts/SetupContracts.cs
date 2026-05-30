using System.ComponentModel.DataAnnotations;

namespace Tubestead.Api.Contracts;

/// <summary>Payload from the first-run setup wizard. Submitting it creates the
/// admin account, persists the chosen settings, and marks setup complete.</summary>
public class SetupRequest
{
    [Required, StringLength(128, MinimumLength = 1)]
    public string SiteName { get; set; } = string.Empty;

    [Required, StringLength(256, MinimumLength = 2)]
    public string AdminUserName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string AdminEmail { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 8)]
    public string AdminPassword { get; set; } = string.Empty;

    [Required]
    public string MediaPath { get; set; } = string.Empty;

    /// <summary>"original-only" (default) or "auto-renditions".</summary>
    public string TranscodeMode { get; set; } = "original-only";
}
