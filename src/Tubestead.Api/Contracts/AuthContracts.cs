using System.ComponentModel.DataAnnotations;

namespace Tubestead.Api.Contracts;

/// <summary>Public view of the signed-in user returned to the SPA.</summary>
public record CurrentUserDto(Guid Id, string UserName, string Email, string? DisplayName, IList<string> Roles);

/// <summary>What the SPA needs on first paint to decide between the setup wizard,
/// the login screen, and the app shell.</summary>
public record AppStatusDto(bool SetupCompleted, string SiteName, CurrentUserDto? User);

public class LoginRequest
{
    [Required]
    public string UserNameOrEmail { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
