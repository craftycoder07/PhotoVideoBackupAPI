using PixNestAPI.Domain.ValueObjects;

namespace PixNestAPI.Domain.Entities;

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public string? ApiKey { get; set; }
    public DateTime RegisteredDate { get; set; } = DateTime.UtcNow;
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    public DeviceSettings Settings { get; set; } = new();
    public BackupStats Stats { get; set; } = new();

    public List<BackupSession> BackupSessions { get; set; } = new();
}
