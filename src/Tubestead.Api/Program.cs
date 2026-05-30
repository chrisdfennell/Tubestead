using System.Text.Json.Serialization;
using Hangfire;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Tubestead.Api.Jobs;
using Tubestead.Api.Uploads;
using Tubestead.Infrastructure;
using Tubestead.Infrastructure.Data;
using tusdotnet;

var builder = WebApplication.CreateBuilder(args);

// Bind the listening port from config when provided (Docker/reverse proxy);
// otherwise fall back to ASPNETCORE_URLS / launch settings. TLS is terminated
// upstream, so we serve plain HTTP internally.
var port = builder.Configuration["TUBESTEAD_PORT"];
if (!string.IsNullOrWhiteSpace(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Resolve the local data directory (db, data-protection keys) once.
var dataPath = builder.Configuration["TUBESTEAD_DATA_PATH"];
if (string.IsNullOrWhiteSpace(dataPath))
    dataPath = Path.Combine(AppContext.BaseDirectory, "data");
Directory.CreateDirectory(dataPath);

// tus buffers in-progress uploads on local disk; the completed file is moved to
// media storage afterwards.
var uploadTempPath = Path.Combine(dataPath, "uploads-temp");
Directory.CreateDirectory(uploadTempPath);
builder.Services.AddSingleton(new UploadStorageOptions(uploadTempPath));

// Resumable uploads send large request bodies; the real cap is enforced by tus
// (uploads.maxBytes setting), not Kestrel.
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = null);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();

// Trust the reverse proxy in front of us (NPM / Cloudflare) for scheme/host/ip.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

// Persist data-protection keys so auth cookies survive restarts/redeploys.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataPath, "keys")))
    .SetApplicationName("Tubestead");

builder.Services.AddTubesteadInfrastructure(builder.Configuration);
builder.AddTubesteadJobs(dataPath);

// Cookie auth tuned for a same-origin SPA: return status codes, never redirect.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "Tubestead.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
});

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

// Static SPA assets (populated in production builds; harmless in dev).
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// Resumable (tus) upload endpoint. Auth + validation are handled in the tus
// event callbacks (admin-only, size/type checks).
app.MapTus(TusUploads.RoutePath, TusUploads.ConfigureAsync);

// Hangfire job dashboard (admin-only) — absent in the Testing environment.
if (!app.Environment.IsEnvironment("Testing"))
{
    app.MapHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new AdminDashboardAuthorizationFilter()],
    });
}

app.MapControllers();

// Any non-API, non-file route is handled by the React router.
app.MapFallbackToFile("index.html");

// Ensure the SQLite directory exists, then migrate + seed roles.
var dbDir = DatabaseOptions.FromConfiguration(app.Configuration).SqliteDirectory();
if (dbDir is not null)
    Directory.CreateDirectory(dbDir);
await app.Services.MigrateAndSeedAsync();

app.Run();

/// <summary>Exposed so integration tests can spin up the API via WebApplicationFactory.</summary>
public partial class Program;
