using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PhotoVideoBackupAPI.Models
{
    public class MediaItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string SessionId { get; set; } = string.Empty;
        
        [Required]
        public string FileName { get; set; } = string.Empty;
        
        public string? OriginalPath { get; set; }
        
        [Required]
        public string ServerPath { get; set; } = string.Empty;
        
        public string FileExtension { get; set; } = string.Empty;
        
        public long FileSize { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        public DateTime? OriginalDate { get; set; }
        
        public DateTime? LastModifiedDate { get; set; }
        
        public MediaType Type { get; set; }
        
        public string? Description { get; set; }
        
        public MediaMetadata Metadata { get; set; } = new();
        
        public BackupStatus Status { get; set; } = BackupStatus.Pending;
        
        public string? ErrorMessage { get; set; }
        
        public string? ThumbnailPath { get; set; }
        
        public bool IsFavorite { get; set; } = false;
        
        public List<string> Tags { get; set; } = new();
        
        // Navigation properties
        [JsonIgnore]
        public BackupSession Session { get; set; } = null!;
    }
    
    public class MediaMetadata
    {
        public int? Width { get; set; }
        
        public int? Height { get; set; }
        
        public string? CameraMake { get; set; }
        
        public string? CameraModel { get; set; }
        
        public double? Latitude { get; set; }
        
        public double? Longitude { get; set; }
        
        public string? Location { get; set; }
        
        public TimeSpan? Duration { get; set; } // For videos
        
        public string? FileHash { get; set; }
        
        public Dictionary<string, string> AdditionalData { get; set; } = new();
    }
    
    public enum MediaType
    {
        Photo,
        Video
    }
    
    public enum BackupStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Skipped
    }
} 