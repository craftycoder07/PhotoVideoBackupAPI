using PixNestAPI.Domain.Enums;
using PixNestAPI.Domain.ValueObjects;

namespace PixNestAPI.Domain.Entities;

public class MediaItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? OriginalPath { get; set; }
    public string ServerPath { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? OriginalDate { get; set; }
    public DateTime? LastModifiedDate { get; set; }
    public MediaType Type { get; set; }
    public string? Description { get; set; }
    public MediaMetadata Metadata { get; set; } = new();
    public BackupStatus Status { get; set; } = BackupStatus.Pending;
    public string? ErrorMessage { get; set; }
    public string? ThumbnailPath { get; set; }
    public bool IsFavorite { get; set; } = false;
    public List<string> Tags { get; set; } = new();

    public BackupSession Session { get; set; } = null!;
}
