using Microsoft.AspNetCore.Identity;

namespace Tubestead.Infrastructure.Identity;

/// <summary>Application user. Uses Guid keys to line up with the rest of the
/// domain model (Video.OwnerId, Playlist.OwnerId).</summary>
public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>Optional friendly name shown in the UI; falls back to UserName.</summary>
    public string? DisplayName { get; set; }

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Application role with Guid keys to match <see cref="ApplicationUser"/>.</summary>
public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() { }
    public ApplicationRole(string name) : base(name) { }
}
