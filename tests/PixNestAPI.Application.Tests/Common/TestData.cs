using PixNestAPI.Domain.Entities;
using PixNestAPI.Domain.Enums;
using PixNestAPI.Domain.ValueObjects;

namespace PixNestAPI.Application.Tests.Common;

/// <summary>
/// Factory helpers that produce domain entities in a valid default state.
/// Tests override only the fields they care about via the optional parameters,
/// keeping the Arrange section focused on what's actually under test.
/// </summary>
internal static class TestData
{
    public static User User(
        string? id = null,
        string username = "alice",
        string email = "alice@example.com",
        string passwordHash = "hashed-password",
        bool isActive = true)
        => new()
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            IsActive = isActive
        };

    public static BackupSession Session(
        string? id = null,
        string? userId = null,
        SessionStatus status = SessionStatus.InProgress)
        => new()
        {
            Id = id ?? Guid.NewGuid().ToString(),
            UserId = userId ?? Guid.NewGuid().ToString(),
            Status = status
        };

    public static MediaItem MediaItem(
        string? id = null,
        string? sessionId = null,
        string fileName = "photo.jpg",
        string serverPath = "/storage/alice/abc.jpg",
        string? thumbnailPath = null,
        MediaType type = MediaType.Photo)
        => new()
        {
            Id = id ?? Guid.NewGuid().ToString(),
            SessionId = sessionId ?? Guid.NewGuid().ToString(),
            FileName = fileName,
            ServerPath = serverPath,
            ThumbnailPath = thumbnailPath,
            Type = type,
            Status = BackupStatus.Completed
        };

    public static BackupSessionInfo SessionInfo(string deviceName = "Pixel 8")
        => new() { DeviceName = deviceName, AppVersion = "1.0.0" };
}
