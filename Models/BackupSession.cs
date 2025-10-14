using System.ComponentModel.DataAnnotations;

namespace PhotoVideoBackupAPI.Models
{
    public class BackupSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string DeviceId { get; set; } = string.Empty;
        
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
    }
    
    public class BackupSessionInfo
    {
        public string? DeviceName { get; set; }
        
        public string? DeviceModel { get; set; }
        
        public string? NetworkType { get; set; } // WiFi, Cellular
        
        public bool IsCharging { get; set; }
        
        public int BatteryLevel { get; set; }
        
        public string? AppVersion { get; set; }
        
        public string? OsVersion { get; set; }
        
        public Dictionary<string, string> AdditionalInfo { get; set; } = new();
    }
    
    public enum SessionStatus
    {
        InProgress,
        Completed,
        Failed,
        Cancelled
    }
} 