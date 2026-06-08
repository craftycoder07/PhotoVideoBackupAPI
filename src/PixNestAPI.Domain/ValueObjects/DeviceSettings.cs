namespace PixNestAPI.Domain.ValueObjects;

public class DeviceSettings
{
    public bool AutoBackupEnabled { get; set; } = true;
    public TimeSpan BackupStartTime { get; set; } = new TimeSpan(22, 0, 0);
    public TimeSpan BackupEndTime { get; set; } = new TimeSpan(6, 0, 0);
    public bool BackupOnlyOnWifi { get; set; } = true;
    public bool BackupOnlyWhenCharging { get; set; } = false;
    public string[] AllowedExtensions { get; set; } = { ".jpg", ".jpeg", ".png", ".gif", ".heic", ".mp4", ".mov" };
    public long MaxFileSize { get; set; } = 100 * 1024 * 1024;
    public bool CompressImages { get; set; } = false;
    public int ImageQuality { get; set; } = 85;
}
