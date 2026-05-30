using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Tubestead.Api.Contracts;
using Tubestead.Domain;
using Tubestead.Infrastructure.Identity;
using Tubestead.Infrastructure.Settings;

namespace Tubestead.Api.Controllers;

/// <summary>First-run setup wizard. All endpoints are anonymous but the mutating
/// one is a no-op (409) once setup has completed, so it can't be used to seize
/// an already-configured instance.</summary>
[ApiController]
[Route("api/setup")]
public class SetupController(
    ISettingsService settings,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : ControllerBase
{
    /// <summary>Values used to pre-fill the wizard form.</summary>
    [HttpGet("defaults")]
    [AllowAnonymous]
    public async Task<ActionResult> GetDefaults(CancellationToken ct)
    {
        if (await settings.IsSetupCompletedAsync(ct))
            return Conflict(new { message = "Setup has already been completed." });

        return Ok(new
        {
            siteName = await settings.GetStringAsync(SettingKeys.SiteName, ct),
            mediaPath = await settings.GetStringAsync(SettingKeys.MediaPath, ct),
            transcodeMode = await settings.GetStringAsync(SettingKeys.TranscodeMode, ct),
        });
    }

    /// <summary>Completes setup: creates the admin account, stores the chosen
    /// settings, marks setup done, and signs the new admin in.</summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<CurrentUserDto>> Complete([FromBody] SetupRequest req, CancellationToken ct)
    {
        if (await settings.IsSetupCompletedAsync(ct))
            return Conflict(new { message = "Setup has already been completed." });

        var mode = req.TranscodeMode is "auto-renditions" ? "auto-renditions" : "original-only";

        // The media path must be creatable/writable on this host.
        try
        {
            Directory.CreateDirectory(req.MediaPath);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(nameof(req.MediaPath),
                $"Media path '{req.MediaPath}' could not be created: {ex.Message}");
            return ValidationProblem(ModelState);
        }

        var user = new ApplicationUser
        {
            UserName = req.AdminUserName,
            Email = req.AdminEmail,
            DisplayName = req.AdminUserName,
            EmailConfirmed = true,
        };

        var create = await userManager.CreateAsync(user, req.AdminPassword);
        if (!create.Succeeded)
        {
            foreach (var e in create.Errors)
                ModelState.AddModelError(string.Empty, e.Description);
            return ValidationProblem(ModelState);
        }

        await userManager.AddToRoleAsync(user, Roles.Admin);

        await settings.SetManyAsync(new Dictionary<string, string?>
        {
            [SettingKeys.SiteName] = req.SiteName,
            [SettingKeys.MediaPath] = req.MediaPath,
            [SettingKeys.TranscodeMode] = mode,
            [SettingKeys.SetupCompleted] = "true",
        }, ct);

        await signInManager.SignInAsync(user, isPersistent: true);

        return Ok(new CurrentUserDto(user.Id, user.UserName!, user.Email!, user.DisplayName, [Roles.Admin]));
    }
}
