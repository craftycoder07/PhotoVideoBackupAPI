using FluentAssertions;
using Moq;
using PixNestAPI.Application.Features.Stats.GetSystemStats;
using PixNestAPI.Application.Interfaces;
using PixNestAPI.Application.Interfaces.Repositories;
using PixNestAPI.Domain.Enums;

namespace PixNestAPI.Application.Tests.Features.Stats;

public class GetSystemStatsHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IMediaRepository> _media = new();
    private readonly Mock<IFileStorageService> _storage = new();

    private GetSystemStatsHandler CreateHandler() => new(_users.Object, _media.Object, _storage.Object);

    [Fact]
    public async Task Handle_AggregatesCountsFromRepositoriesAndStorage()
    {
        var lastSeen = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        _users.Setup(r => r.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(10);
        _users.Setup(r => r.CountActiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(6);
        _users.Setup(r => r.GetLastSeenAsync(It.IsAny<CancellationToken>())).ReturnsAsync(lastSeen);
        _media.Setup(r => r.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(100);
        _media.Setup(r => r.CountByTypeAsync(MediaType.Photo, It.IsAny<CancellationToken>())).ReturnsAsync(70);
        _media.Setup(r => r.CountByTypeAsync(MediaType.Video, It.IsAny<CancellationToken>())).ReturnsAsync(30);
        _media.Setup(r => r.SumFileSizeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(999_999L);
        _storage.Setup(s => s.GetAvailableStorage()).Returns(500_000L);

        var result = await CreateHandler().Handle(new GetSystemStatsQuery(), CancellationToken.None);

        result.TotalUsers.Should().Be(10);
        result.ActiveUsers.Should().Be(6);
        result.LastBackupActivity.Should().Be(lastSeen);
        result.TotalMediaItems.Should().Be(100);
        result.TotalPhotos.Should().Be(70);
        result.TotalVideos.Should().Be(30);
        result.TotalStorageUsed.Should().Be(999_999L);
        result.AvailableStorage.Should().Be(500_000L);
    }
}
