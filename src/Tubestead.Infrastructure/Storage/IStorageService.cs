namespace Tubestead.Infrastructure.Storage;

/// <summary>Resolves where media lives on disk. The media root comes from settings
/// (the wizard / admin UI), so this is scoped and async. Paths stored in the DB are
/// always relative to the media root, so the library survives the share being
/// re-mounted at a different absolute path.</summary>
public interface IStorageService
{
    /// <summary>Absolute media root (the NAS share), ensured to exist.</summary>
    Task<string> GetMediaRootAsync(CancellationToken ct = default);

    /// <summary>Ensures and returns the absolute directory for one video's files.</summary>
    Task<string> EnsureVideoDirectoryAsync(Guid videoId, CancellationToken ct = default);

    /// <summary>Relative (DB-stored) path of the original file, e.g.
    /// "videos/{id}/original.mp4".</summary>
    string OriginalRelativePath(Guid videoId, string extension);

    /// <summary>Resolves a DB-stored relative path to an absolute path on disk.</summary>
    Task<string> ResolveAsync(string relativePath, CancellationToken ct = default);

    /// <summary>Deletes a video's directory and everything under it (best effort).</summary>
    Task DeleteVideoDirectoryAsync(Guid videoId, CancellationToken ct = default);
}
