using Tubestead.Infrastructure.Settings;

namespace Tubestead.Infrastructure.Storage;

/// <inheritdoc />
public class StorageService(ISettingsService settings) : IStorageService
{
    public async Task<string> GetMediaRootAsync(CancellationToken ct = default)
    {
        var root = await settings.GetStringAsync(SettingKeys.MediaPath, ct);
        Directory.CreateDirectory(root);
        return root;
    }

    public async Task<string> EnsureVideoDirectoryAsync(Guid videoId, CancellationToken ct = default)
    {
        var root = await GetMediaRootAsync(ct);
        var dir = Path.Combine(root, "videos", videoId.ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    public string OriginalRelativePath(Guid videoId, string extension)
    {
        var ext = NormalizeExtension(extension);
        // Forward slashes so the stored value is stable across OSes.
        return $"videos/{videoId:N}/original{ext}";
    }

    public async Task<string> ResolveAsync(string relativePath, CancellationToken ct = default)
    {
        var root = await GetMediaRootAsync(ct);
        var normalized = relativePath.Replace('\\', Path.DirectorySeparatorChar)
                                     .Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(root, normalized));
    }

    public async Task DeleteVideoDirectoryAsync(Guid videoId, CancellationToken ct = default)
    {
        var root = await GetMediaRootAsync(ct);
        var dir = Path.Combine(root, "videos", videoId.ToString("N"));
        if (Directory.Exists(dir))
        {
            try { Directory.Delete(dir, recursive: true); }
            catch { /* best effort */ }
        }
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension)) return string.Empty;
        var ext = extension.Trim().ToLowerInvariant();
        return ext.StartsWith('.') ? ext : "." + ext;
    }
}
