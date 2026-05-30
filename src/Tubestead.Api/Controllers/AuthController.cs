using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Tubestead.Api.Contracts;
using Tubestead.Infrastructure.Identity;
using Tubestead.Infrastructure.Settings;

namespace Tubestead.Api.Controllers;

/// <summary>Cookie-based authentication for the same-origin SPA.</summary>
[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ISettingsService settings) : ControllerBase
{
    /// <summary>One call the SPA makes on load: setup state, branding, and the
    /// current user (null if signed out).</summary>
    [HttpGet("status")]
    [AllowAnonymous]
    public async Task<ActionResult<AppStatusDto>> Status(CancellationToken ct)
    {
        var siteName = await settings.GetStringAsync(SettingKeys.SiteName, ct);
        var setupCompleted = await settings.IsSetupCompletedAsync(ct);

        CurrentUserDto? me = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await userManager.GetUserAsync(User);
            if (user is not null)
                me = await ToDtoAsync(user);
        }

        return Ok(new AppStatusDto(setupCompleted, siteName, me));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserDto>> Me()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();
        return Ok(await ToDtoAsync(user));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<CurrentUserDto>> Login([FromBody] LoginRequest req)
    {
        var user = req.UserNameOrEmail.Contains('@')
            ? await userManager.FindByEmailAsync(req.UserNameOrEmail)
            : await userManager.FindByNameAsync(req.UserNameOrEmail);

        if (user is null)
            return Unauthorized(new { message = "Invalid credentials." });

        var result = await signInManager.PasswordSignInAsync(
            user, req.Password, req.RememberMe, lockoutOnFailure: true);

        if (result.IsLockedOut)
            return StatusCode(StatusCodes.Status423Locked, new { message = "Account temporarily locked. Try again later." });
        if (!result.Succeeded)
            return Unauthorized(new { message = "Invalid credentials." });

        return Ok(await ToDtoAsync(user));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }

    private async Task<CurrentUserDto> ToDtoAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new CurrentUserDto(user.Id, user.UserName!, user.Email ?? string.Empty, user.DisplayName, roles);
    }
}
