using PixNestAPI.Domain.Enums;
using PixNestAPI.Domain.ValueObjects;

namespace PixNestAPI.Application.Features.Sessions.Dtos;

public class BackupSessionDto
{
    public string Id { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime? EndTime { get; init; }
    public SessionStatus Status { get; init; }
    public int TotalItems { get; init; }
    public int ProcessedItems { get; init; }
    public int SuccessfulBackups { get; init; }
    public int FailedBackups { get; init; }
    public int SkippedItems { get; init; }
    public long TotalSize { get; init; }
    public string? ErrorMessage { get; init; }
    public List<string> Errors { get; init; } = new();
    public BackupSessionInfo SessionInfo { get; init; } = new();
}
