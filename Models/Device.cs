using System.ComponentModel.DataAnnotations;

namespace PhotoVideoBackupAPI.Models
{
    public class Device
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        public string DeviceName { get; set; } = string.Empty;
        
        [Required]
        public string DeviceModel { get; set; } = string.Empty;
        
        public string? DeviceId { get; set; } // Unique device identifier
        
        public string? ApiKey { get; set; } // For authentication (legacy)
        
        public DateTime RegisteredDate { get; set; } = DateTime.UtcNow;
        
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
        
        public bool IsActive { get; set; } = true;
        
        public DeviceSettings Settings { get; set; } = new();
        
        public List<MediaItem> MediaItems { get; set; } = new();
        
        public BackupStats Stats { get; set; } = new();
        
        // Navigation properties
        public User User { get; set; } = null!;
    }
    
    public class DeviceSettings
    {
        public bool AutoBackupEnabled { get; set; } = true;
        
        public TimeSpan BackupStartTime { get; set; } = new TimeSpan(22, 0, 0); // 10 PM
        
        public TimeSpan BackupEndTime { get; set; } = new TimeSpan(6, 0, 0); // 6 AM
        
        public bool BackupOnlyOnWifi { get; set; } = true;
        
        public bool BackupOnlyWhenCharging { get; set; } = false;
        
        public string[] AllowedExtensions { get; set; } = { ".jpg", ".jpeg", ".png", ".gif", ".heic", ".mp4", ".mov" };
        
        public long MaxFileSize { get; set; } = 100 * 1024 * 1024; // 100MB
        
        public bool CompressImages { get; set; } = false;
        
        public int ImageQuality { get; set; } = 85; // 0-100
    }
    
    public class BackupStats
    {
        public int TotalPhotos { get; set; }
        
        public int TotalVideos { get; set; }
        
        public long TotalSize { get; set; }
        
        public DateTime LastBackupDate { get; set; }
        
        public int FailedBackups { get; set; }
        
        public int SuccessfulBackups { get; set; }
    }
} 