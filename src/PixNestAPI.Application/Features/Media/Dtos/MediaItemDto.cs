using PixNestAPI.Domain.Enums;
using PixNestAPI.Domain.ValueObjects;

namespace PixNestAPI.Application.Features.Media.Dtos;

public class MediaItemDto
{
    public string Id { get; init; } = string.Empty;
    public string SessionId { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public string FileExtension { get; init; } = string.Empty;
    public MediaType Type { get; init; }
    public BackupStatus Status { get; init; }
    public DateTime CreatedDate { get; init; }
    public DateTime? OriginalDate { get; init; }
    public string? ThumbnailPath { get; init; }
    public bool IsFavorite { get; init; }
    public List<string> Tags { get; init; } = new();
    public MediaMetadata Metadata { get; init; } = new();
    public string? Description { get; init; }
    public string? ErrorMessage { get; init; }
}
