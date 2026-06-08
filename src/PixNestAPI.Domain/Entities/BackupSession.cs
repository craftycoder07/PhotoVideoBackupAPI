using PixNestAPI.Domain.Enums;
using PixNestAPI.Domain.ValueObjects;

namespace PixNestAPI.Domain.Entities;

public class BackupSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.InProgress;
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public int SuccessfulBackups { get; set; }
    public int FailedBackups { get; set; }
    public int SkippedItems { get; set; }
    public long TotalSize { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Errors { get; set; } = new();
    public BackupSessionInfo SessionInfo { get; set; } = new();

    public List<MediaItem> Items { get; set; } = new();
    public User User { get; set; } = null!;
}
