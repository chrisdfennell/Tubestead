using Tubestead.Api.Uploads;
using Xunit;

namespace Tubestead.Tests;

public class UploadValidationTests
{
    private const string Allowed = ".mp4,.mov,.mkv";

    [Fact]
    public void Accepts_allowed_extension_within_size()
    {
        Assert.Null(UploadValidation.Validate("clip.mp4", 1_000, 10_000, Allowed));
    }

    [Fact]
    public void Rejects_oversized_upload()
    {
        var error = UploadValidation.Validate("clip.mp4", 20_000, 10_000, Allowed);
        Assert.Contains("maximum allowed size", error);
    }

    [Fact]
    public void Rejects_disallowed_extension()
    {
        var error = UploadValidation.Validate("notes.txt", 100, 10_000, Allowed);
        Assert.Contains("not allowed", error);
    }

    [Fact]
    public void Rejects_missing_filename()
    {
        Assert.Contains("filename", UploadValidation.Validate("", 100, 10_000, Allowed));
    }

    [Fact]
    public void Extension_check_is_case_insensitive()
    {
        Assert.Null(UploadValidation.Validate("CLIP.MP4", 100, 10_000, Allowed));
    }

    [Fact]
    public void Zero_max_means_no_size_limit()
    {
        Assert.Null(UploadValidation.Validate("clip.mp4", 999_999_999, 0, Allowed));
    }

    [Fact]
    public void Empty_allowlist_permits_any_extension()
    {
        Assert.Null(UploadValidation.Validate("clip.xyz", 100, 10_000, ""));
    }
}
