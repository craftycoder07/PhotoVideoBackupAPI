namespace PixNestAPI.Domain.ValueObjects;

public class BackupSessionInfo
{
    public string? DeviceName { get; set; }
    public string? DeviceModel { get; set; }
    public string? NetworkType { get; set; }
    public bool IsCharging { get; set; }
    public int BatteryLevel { get; set; }
    public string? AppVersion { get; set; }
    public string? OsVersion { get; set; }
    public Dictionary<string, string> AdditionalInfo { get; set; } = new();
}
