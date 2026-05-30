using Tubestead.Domain;

namespace Tubestead.Api.Contracts;

/// <summary>Summary card for the library grid.</summary>
public record VideoListItemDto(
    Guid Id,
    string Title,
    VideoStatus Status,
    string? StatusMessage,
    double? DurationSeconds,
    string? ThumbnailUrl,
    DateTimeOffset CreatedUtc);

public record RenditionDto(
    Guid Id,
    string Label,
    string Format,
    int? Width,
    int? Height,
    long? SizeBytes,
    bool IsOriginal);

/// <summary>Full video page payload.</summary>
public record VideoDetailDto(
    Guid Id,
    string Title,
    string? Description,
    VideoStatus Status,
    string? StatusMessage,
    double? DurationSeconds,
    int? Width,
    int? Height,
    string? ThumbnailUrl,
    string? OriginalFileName,
    DateTimeOffset CreatedUtc,
    IReadOnlyList<RenditionDto> Renditions);
