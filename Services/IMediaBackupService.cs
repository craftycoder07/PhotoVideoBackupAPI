using PhotoVideoBackupAPI.Models;

namespace PhotoVideoBackupAPI.Services
{
    public interface IMediaBackupService
    {
        // Device management
        Task<Device> RegisterDeviceAsync(DeviceRegistrationRequest request);
        Task<Device?> GetDeviceAsync(string deviceId);
        Task<List<Device>> GetAllDevicesAsync();
        Task<Device> UpdateDeviceSettingsAsync(string deviceId, DeviceSettings settings);
        Task<bool> DeleteDeviceAsync(string deviceId);
        
        // Backup sessions
        Task<BackupSession> StartBackupSessionAsync(string deviceId, BackupSessionInfo sessionInfo);
        Task<BackupSession> GetBackupSessionAsync(string sessionId);
        Task<BackupSession> UpdateBackupSessionAsync(string sessionId, BackupSessionUpdateRequest request);
        Task<List<BackupSession>> GetDeviceBackupSessionsAsync(string deviceId);
        
        // Media upload
        Task<MediaItem> UploadMediaAsync(string sessionId, IFormFile file, MediaMetadata? metadata = null);
        Task<MediaItem> GetMediaItemAsync(string mediaId);
        Task<List<MediaItem>> GetDeviceMediaAsync(string deviceId, int page = 1, int pageSize = 50);
        Task<bool> DeleteMediaItemAsync(string mediaId);
        
        // Thumbnails
        Task<string> GenerateThumbnailAsync(string mediaId);
        Task<byte[]> GetThumbnailAsync(string mediaId);
        
        // Statistics
        Task<BackupStats> GetDeviceStatsAsync(string deviceId);
        Task<SystemStats> GetSystemStatsAsync();
        
        // Search and filtering
        Task<List<MediaItem>> SearchMediaAsync(string deviceId, string query, DateTime? fromDate = null, DateTime? toDate = null);
        Task<List<MediaItem>> GetMediaByDateRangeAsync(string deviceId, DateTime fromDate, DateTime toDate);
    }
    
    public class DeviceRegistrationRequest
    {
        public string DeviceName { get; set; } = string.Empty;
        public string DeviceModel { get; set; } = string.Empty;
        public string? DeviceId { get; set; }
        public DeviceSettings? Settings { get; set; }
    }
    
    public class BackupSessionUpdateRequest
    {
        public int? ProcessedItems { get; set; }
        public int? SuccessfulBackups { get; set; }
        public int? FailedBackups { get; set; }
        public int? SkippedItems { get; set; }
        public long? TotalSize { get; set; }
        public SessionStatus? Status { get; set; }
        public string? ErrorMessage { get; set; }
    }
    
    public class SystemStats
    {
        public int TotalDevices { get; set; }
        public int ActiveDevices { get; set; }
        public long TotalStorageUsed { get; set; }
        public long AvailableStorage { get; set; }
        public int TotalMediaItems { get; set; }
        public int TotalPhotos { get; set; }
        public int TotalVideos { get; set; }
        public DateTime LastBackupActivity { get; set; }
    }
} 