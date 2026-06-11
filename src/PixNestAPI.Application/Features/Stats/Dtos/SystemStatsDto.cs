namespace PixNestAPI.Application.Features.Stats.Dtos;

public class SystemStatsDto
{
    public int TotalUsers { get; init; }
    public int ActiveUsers { get; init; }
    public long TotalStorageUsed { get; init; }
    public long AvailableStorage { get; init; }
    public int TotalMediaItems { get; init; }
    public int TotalPhotos { get; init; }
    public int TotalVideos { get; init; }
    public DateTime LastBackupActivity { get; init; }
}
