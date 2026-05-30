namespace Tubestead.Domain;

/// <summary>Lifecycle state of an uploaded video.</summary>
public enum VideoStatus
{
    /// <summary>Bytes are still being received (resumable upload in progress).</summary>
    Uploading = 0,
    /// <summary>Upload complete; metadata/thumbnail/renditions are being produced.</summary>
    Processing = 1,
    /// <summary>Playable: at least the original is available for streaming/download.</summary>
    Ready = 2,
    /// <summary>Processing failed; see <c>Video.StatusMessage</c>.</summary>
    Failed = 3,
}

/// <summary>Application role names. Kept as constants so they read the same in
/// policies, attributes, and seed code.</summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Viewer = "Viewer";

    public static readonly string[] All = [Admin, Viewer];
}
