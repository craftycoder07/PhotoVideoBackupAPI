using PhotoVideoBackupAPI.Models;

namespace PhotoVideoBackupAPI.Services
{
    public interface IMediaBackupService
    {
        // User settings management
        Task<User> UpdateUserSettingsAsync(string userId, DeviceSettings settings);
        Task<User?> GetUserAsync(string userId);
        
        // Backup sessions
        Task<BackupSession> StartBackupSessionAsync(string userId, BackupSessionInfo sessionInfo);
        Task<BackupSession> GetBackupSessionAsync(string sessionId);
        Task<BackupSession> UpdateBackupSessionAsync(string sessionId, BackupSessionUpdateRequest request);
        Task<List<BackupSession>> GetUserBackupSessionsAsync(string userId);
        
        // Media upload
        Task<MediaItem> UploadMediaAsync(string sessionId, IFormFile file, MediaMetadata? metadata = null);
        Task<MediaItem> GetMediaItemAsync(string mediaId);
        Task<List<MediaItem>> GetUserMediaAsync(string userId, int page = 1, int pageSize = 50);
        Task<bool> DeleteMediaItemAsync(string mediaId);
        
        // Thumbnails
        Task<string> GenerateThumbnailAsync(string mediaId);
        Task<byte[]> GetThumbnailAsync(string mediaId);
        
        // Statistics
        Task<BackupStats> GetUserStatsAsync(string userId);
        Task<SystemStats> GetSystemStatsAsync();
        
        // Search and filtering
        Task<List<MediaItem>> SearchUserMediaAsync(string userId, string query, DateTime? fromDate = null, DateTime? toDate = null);
        Task<List<MediaItem>> GetMediaByDateRangeAsync(string userId, DateTime fromDate, DateTime toDate);
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
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public long TotalStorageUsed { get; set; }
        public long AvailableStorage { get; set; }
        public int TotalMediaItems { get; set; }
        public int TotalPhotos { get; set; }
        public int TotalVideos { get; set; }
        public DateTime LastBackupActivity { get; set; }
    }
} 