namespace Tubestead.Api.Uploads;

/// <summary>Pure upload validation rules, factored out of the tus callback so they
/// can be unit-tested without the tus pipeline.</summary>
public static class UploadValidation
{
    /// <summary>Returns an error message if the upload should be rejected, or null
    /// if it's acceptable.</summary>
    public static string? Validate(string? fileName, long uploadLength, long maxBytes, string allowedExtensionsCsv)
    {
        if (maxBytes > 0 && uploadLength > maxBytes)
            return $"File exceeds the maximum allowed size of {maxBytes} bytes.";

        if (string.IsNullOrWhiteSpace(fileName))
            return "A 'filename' metadata value is required.";

        var allowed = ParseExtensions(allowedExtensionsCsv);
        var ext = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
        if (allowed.Count > 0 && !allowed.Contains(ext))
            return $"File type '{ext}' is not allowed.";

        return null;
    }

    public static HashSet<string> ParseExtensions(string csv) =>
        csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(e => e.StartsWith('.') ? e.ToLowerInvariant() : "." + e.ToLowerInvariant())
            .ToHashSet();
}
