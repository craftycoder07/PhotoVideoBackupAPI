using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PhotoVideoBackupAPI.Models
{
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
    
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
        
        public bool IsActive { get; set; } = true;
        
        // Device-related fields moved from Device table
        public string? ApiKey { get; set; } // For authentication (legacy)
        
        public DateTime RegisteredDate { get; set; } = DateTime.UtcNow;
        
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
        
        public DeviceSettings Settings { get; set; } = new();
        
        public BackupStats Stats { get; set; } = new();
        
        // Navigation properties
        [JsonIgnore]
        public List<BackupSession> BackupSessions { get; set; } = new();
    }
    
    public class UserRegistrationRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;
    }
    
    public class UserLoginRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
    }
    
    public class AuthResponse
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
    
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}


