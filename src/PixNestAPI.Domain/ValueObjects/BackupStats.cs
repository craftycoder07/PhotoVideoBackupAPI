namespace PixNestAPI.Domain.ValueObjects;

public class BackupStats
{
    public int TotalPhotos { get; set; }
    public int TotalVideos { get; set; }
    public long TotalSize { get; set; }
    public DateTime LastBackupDate { get; set; }
    public int FailedBackups { get; set; }
    public int SuccessfulBackups { get; set; }
}
