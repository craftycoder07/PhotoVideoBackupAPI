using System.ComponentModel.DataAnnotations;

namespace PhotoVideoBackupAPI.Models
{
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
        
        // Navigation properties
        public List<Device> Devices { get; set; } = new();
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

