using System.Net;
using System.Net.Http.Json;
using Tubestead.Api.Contracts;
using Tubestead.Domain;
using Xunit;

namespace Tubestead.Tests;

/// <summary>End-to-end happy path for first-run setup and cookie auth, plus the
/// guards: setup can't be re-run, and protected endpoints reject anonymous calls.</summary>
public class SetupFlowTests
{
    private SetupRequest ValidSetup(TubesteadApiFactory f) => new()
    {
        SiteName = "Test Tube",
        AdminUserName = "admin",
        AdminEmail = "admin@example.com",
        AdminPassword = "homestead123",
        MediaPath = f.MediaPath,
        TranscodeMode = "original-only",
    };

    [Fact]
    public async Task Fresh_instance_reports_setup_incomplete()
    {
        using var factory = new TubesteadApiFactory();
        var client = factory.CreateClient();

        var status = await client.GetFromJsonAsync<AppStatusDto>("/api/auth/status");

        Assert.NotNull(status);
        Assert.False(status!.SetupCompleted);
        Assert.Null(status.User);
    }

    [Fact]
    public async Task Completing_setup_creates_admin_and_signs_in()
    {
        using var factory = new TubesteadApiFactory();
        var client = factory.CreateClient();

        var res = await client.PostAsJsonAsync("/api/setup", ValidSetup(factory));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var user = await res.Content.ReadFromJsonAsync<CurrentUserDto>();
        Assert.NotNull(user);
        Assert.Contains(Roles.Admin, user!.Roles);

        // The setup-issued cookie keeps us signed in.
        var me = await client.GetFromJsonAsync<CurrentUserDto>("/api/auth/me");
        Assert.Equal("admin", me!.UserName);

        var status = await client.GetFromJsonAsync<AppStatusDto>("/api/auth/status");
        Assert.True(status!.SetupCompleted);
        Assert.Equal("Test Tube", status.SiteName);
    }

    [Fact]
    public async Task Setup_cannot_be_run_twice()
    {
        using var factory = new TubesteadApiFactory();
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/setup", ValidSetup(factory));
        var second = await client.PostAsJsonAsync("/api/setup", ValidSetup(factory));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Protected_endpoint_rejects_anonymous()
    {
        using var factory = new TubesteadApiFactory();
        var client = factory.CreateClient();

        var res = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Login_succeeds_with_correct_credentials_and_fails_otherwise()
    {
        using var factory = new TubesteadApiFactory();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/setup", ValidSetup(factory));

        // Fresh client without the setup cookie.
        var fresh = factory.CreateClient();

        var bad = await fresh.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { UserNameOrEmail = "admin", Password = "wrong-password" });
        Assert.Equal(HttpStatusCode.Unauthorized, bad.StatusCode);

        var ok = await fresh.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { UserNameOrEmail = "admin", Password = "homestead123" });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        var me = await fresh.GetFromJsonAsync<CurrentUserDto>("/api/auth/me");
        Assert.Equal("admin@example.com", me!.Email);
    }
}
