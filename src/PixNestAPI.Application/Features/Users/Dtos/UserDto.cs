using PixNestAPI.Domain.ValueObjects;

namespace PixNestAPI.Application.Features.Users.Dtos;

public class UserDto
{
    public string Id { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastLoginAt { get; init; }
    public DateTime LastSeen { get; init; }
    public DeviceSettings Settings { get; init; } = new();
    public BackupStats Stats { get; init; } = new();
}
