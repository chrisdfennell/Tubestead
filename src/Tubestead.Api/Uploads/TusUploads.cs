using System.Net;
using System.Security.Claims;
using System.Text;
using Tubestead.Domain;
using Tubestead.Domain.Entities;
using Tubestead.Infrastructure.Data;
using Tubestead.Infrastructure.Processing;
using Tubestead.Infrastructure.Settings;
using Tubestead.Infrastructure.Storage;
using tusdotnet.Interfaces;
using tusdotnet.Models;
using tusdotnet.Models.Configuration;
using tusdotnet.Stores;

namespace Tubestead.Api.Uploads;

/// <summary>Builds the tus (resumable upload) configuration and wires the
/// completion handler that turns a finished upload into a Video + original
/// rendition and queues it for processing. Uploads are admin-only.</summary>
public static class TusUploads
{
    public const string RoutePath = "/api/uploads";

    public static Task<DefaultTusConfiguration> ConfigureAsync(HttpContext httpContext)
    {
        var services = httpContext.RequestServices;
        var options = services.GetRequiredService<UploadStorageOptions>();
        Directory.CreateDirectory(options.TempPath);

        var config = new DefaultTusConfiguration
        {
            Store = new TusDiskStore(options.TempPath),
            Events = new Events
            {
                OnAuthorizeAsync = OnAuthorize,
                OnBeforeCreateAsync = OnBeforeCreate,
                OnFileCompleteAsync = OnFileComplete,
            },
        };

        return Task.FromResult(config);
    }

    /// <summary>Only signed-in admins may upload.</summary>
    private static Task OnAuthorize(AuthorizeContext ctx)
    {
        var user = ctx.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
            ctx.FailRequest(HttpStatusCode.Unauthorized);
        else if (!user.IsInRole(Roles.Admin))
            ctx.FailRequest(HttpStatusCode.Forbidden);
        return Task.CompletedTask;
    }

    /// <summary>Reject oversized uploads and disallowed file types before any bytes
    /// are stored.</summary>
    private static async Task OnBeforeCreate(BeforeCreateContext ctx)
    {
        var settings = ctx.HttpContext.RequestServices.GetRequiredService<ISettingsService>();
        var maxBytes = await settings.GetLongAsync(SettingKeys.MaxUploadBytes);
        var allowedCsv = await settings.GetStringAsync(SettingKeys.AllowedExtensions);
        var fileName = GetMetadata(ctx.Metadata, "filename");

        var error = UploadValidation.Validate(fileName, ctx.UploadLength, maxBytes, allowedCsv);
        if (error is not null)
            ctx.FailRequest(error);
    }

    /// <summary>Move the finished upload into media storage, create the Video and its
    /// original rendition, and enqueue processing.</summary>
    private static async Task OnFileComplete(FileCompleteContext ctx)
    {
        var services = ctx.HttpContext.RequestServices;
        var db = services.GetRequiredService<TubesteadDbContext>();
        var storage = services.GetRequiredService<IStorageService>();
        var jobs = services.GetRequiredService<IVideoJobQueue>();

        var file = await ctx.GetFileAsync();
        var metadata = await file.GetMetadataAsync(ctx.CancellationToken);

        var originalName = ReadString(metadata, "filename") ?? "upload";
        var title = ReadString(metadata, "title");
        if (string.IsNullOrWhiteSpace(title))
            title = Path.GetFileNameWithoutExtension(originalName);

        var ownerId = GetUserId(ctx.HttpContext.User);
        var ext = Path.GetExtension(originalName);

        var video = new Video
        {
            Id = Guid.NewGuid(),
            Title = title,
            OwnerId = ownerId,
            Status = VideoStatus.Processing,
            StatusMessage = "Queued for processing…",
            OriginalFileName = originalName,
        };

        // Move the buffered upload into the media share at videos/{id}/original.ext.
        await storage.EnsureVideoDirectoryAsync(video.Id, ctx.CancellationToken);
        var relativePath = storage.OriginalRelativePath(video.Id, ext);
        var destPath = await storage.ResolveAsync(relativePath, ctx.CancellationToken);

        await using (var source = await file.GetContentAsync(ctx.CancellationToken))
        await using (var dest = File.Create(destPath))
        {
            await source.CopyToAsync(dest, ctx.CancellationToken);
        }

        var size = new FileInfo(destPath).Length;
        video.OriginalSizeBytes = size;
        video.Renditions.Add(new MediaRendition
        {
            Label = "Original",
            Format = ext.TrimStart('.').ToLowerInvariant(),
            Path = relativePath,
            SizeBytes = size,
            IsOriginal = true,
        });

        db.Videos.Add(video);
        await db.SaveChangesAsync(ctx.CancellationToken);

        // Reclaim the temp upload now that it's safely in media storage.
        if (ctx.Store is ITusTerminationStore termination)
            await termination.DeleteFileAsync(file.Id, ctx.CancellationToken);

        jobs.EnqueueProcessing(video.Id);
    }

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }

    private static string? GetMetadata(IDictionary<string, Metadata> metadata, string key) =>
        metadata.TryGetValue(key, out var value) ? value.GetString(Encoding.UTF8) : null;

    private static string? ReadString(Dictionary<string, Metadata> metadata, string key) =>
        metadata.TryGetValue(key, out var value) && !value.HasEmptyValue
            ? value.GetString(Encoding.UTF8)
            : null;
}
