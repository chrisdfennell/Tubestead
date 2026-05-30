namespace Tubestead.Api.Uploads;

/// <summary>Where in-progress tus uploads are buffered. This is on LOCAL disk
/// (the data volume), never the NAS share — tus needs fast random-access writes
/// and the completed file is moved into media storage afterwards.</summary>
public sealed record UploadStorageOptions(string TempPath);
